using System.Data;
using Dapper;
using FinTrack.API.Models;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;

namespace FinTrack.API.Services;

public class DatabaseService
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseService> _logger;

    public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        _logger = logger;
    }

    public IDbConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }

    public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? parameters = null)
    {
        using var connection = CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<T>(sql, parameters);
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null)
    {
        using var connection = CreateConnection();
        return await connection.QueryAsync<T>(sql, parameters);
    }

    public async Task<int> ExecuteAsync(string sql, object? parameters = null)
    {
        using var connection = CreateConnection();
        return await connection.ExecuteAsync(sql, parameters);
    }

    public async Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null)
    {
        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<T>(sql, parameters);
    }

    public async Task<(IEnumerable<T1> First, IEnumerable<T2> Second)> QueryMultipleAsync<T1, T2>(
        string sql, object? parameters = null)
    {
        using var connection = CreateConnection();
        using var multi = await connection.QueryMultipleAsync(sql, parameters);
        var first = await multi.ReadAsync<T1>();
        var second = await multi.ReadAsync<T2>();
        return (first, second);
    }

    public async Task<IEnumerable<T>> QueryRawAsync<T>(string rawSql)
    {
        using var connection = CreateConnection();
        _logger.LogDebug("Executing raw query");
        return await connection.QueryAsync<T>(rawSql);
    }

    public T? DeserializePayload<T>(string json)
    {
        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            MaxDepth = 64
        };
        return JsonConvert.DeserializeObject<T>(json, settings);
    }

    public async Task<bool> InitializeDatabaseAsync()
    {
        try
        {
            using var connection = CreateConnection();
            connection.Open();

            var createTables = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
                CREATE TABLE Users (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Email NVARCHAR(255) NOT NULL UNIQUE,
                    FullName NVARCHAR(255) NOT NULL,
                    PasswordHash NVARCHAR(500) NOT NULL,
                    Role NVARCHAR(50) DEFAULT 'user',
                    IsAdmin BIT DEFAULT 0,
                    IsActive BIT DEFAULT 1,
                    PhoneNumber NVARCHAR(20),
                    ProfilePictureUrl NVARCHAR(500),
                    Currency NVARCHAR(10) DEFAULT 'USD',
                    TimeZone NVARCHAR(100) DEFAULT 'UTC',
                    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
                    LastLoginAt DATETIME2
                );

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Accounts' AND xtype='U')
                CREATE TABLE Accounts (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    UserId INT NOT NULL REFERENCES Users(Id),
                    Name NVARCHAR(255) NOT NULL,
                    Type NVARCHAR(50) NOT NULL,
                    Balance DECIMAL(18,2) DEFAULT 0,
                    CreditLimit DECIMAL(18,2) DEFAULT 0,
                    Currency NVARCHAR(10) DEFAULT 'USD',
                    Institution NVARCHAR(255),
                    AccountNumber NVARCHAR(100),
                    IsActive BIT DEFAULT 1,
                    Color NVARCHAR(20),
                    Icon NVARCHAR(100),
                    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
                    UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
                );

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Transactions' AND xtype='U')
                CREATE TABLE Transactions (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    UserId INT NOT NULL REFERENCES Users(Id),
                    AccountId INT NOT NULL REFERENCES Accounts(Id),
                    Description NVARCHAR(500) NOT NULL,
                    Amount DECIMAL(18,2) NOT NULL,
                    Type NVARCHAR(50) NOT NULL,
                    Category NVARCHAR(100) NOT NULL,
                    SubCategory NVARCHAR(100),
                    TransactionDate DATETIME2 NOT NULL,
                    Notes NVARCHAR(1000),
                    Tags NVARCHAR(500),
                    IsRecurring BIT DEFAULT 0,
                    RecurrencePattern NVARCHAR(100),
                    MerchantName NVARCHAR(255),
                    ReferenceNumber NVARCHAR(100),
                    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
                    UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
                );

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Budgets' AND xtype='U')
                CREATE TABLE Budgets (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    UserId INT NOT NULL REFERENCES Users(Id),
                    Name NVARCHAR(255) NOT NULL,
                    Category NVARCHAR(100) NOT NULL,
                    Amount DECIMAL(18,2) NOT NULL,
                    SpentAmount DECIMAL(18,2) DEFAULT 0,
                    Period NVARCHAR(50) NOT NULL,
                    StartDate DATETIME2 NOT NULL,
                    EndDate DATETIME2 NOT NULL,
                    IsActive BIT DEFAULT 1,
                    AlertEnabled BIT DEFAULT 1,
                    AlertThresholdPercent INT DEFAULT 80,
                    Color NVARCHAR(20),
                    Notes NVARCHAR(1000),
                    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
                    UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
                );

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Webhooks' AND xtype='U')
                CREATE TABLE Webhooks (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    UserId INT NOT NULL REFERENCES Users(Id),
                    Name NVARCHAR(255) NOT NULL,
                    Url NVARCHAR(2000) NOT NULL,
                    EventType NVARCHAR(100) NOT NULL,
                    IsActive BIT DEFAULT 1,
                    Secret NVARCHAR(500),
                    RetryCount INT DEFAULT 3,
                    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
                    LastTriggeredAt DATETIME2,
                    LastDeliverySucceeded BIT
                );

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Transfers' AND xtype='U')
                CREATE TABLE Transfers (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    UserId INT NOT NULL REFERENCES Users(Id),
                    SourceAccountId INT NOT NULL REFERENCES Accounts(Id),
                    DestinationAccountId INT NOT NULL REFERENCES Accounts(Id),
                    Amount DECIMAL(18,2) NOT NULL,
                    Description NVARCHAR(500),
                    TransferDate DATETIME2 NOT NULL,
                    Notes NVARCHAR(1000),
                    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
                );
            ";

            await connection.ExecuteAsync(createTables);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize database");
            return false;
        }
    }
}
