using FinTrack.API.Models;

namespace FinTrack.API.Services;

public class AccountService
{
    private readonly DatabaseService _db;
    private readonly ILogger<AccountService> _logger;

    public AccountService(DatabaseService db, ILogger<AccountService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<Account>> GetUserAccountsAsync(int userId)
    {
        return await _db.QueryAsync<Account>(
            "SELECT * FROM Accounts WHERE UserId = @UserId AND IsActive = 1 ORDER BY Name",
            new { UserId = userId });
    }

    public async Task<Account?> GetAccountByIdAsync(int accountId)
    {
        return await _db.QueryFirstOrDefaultAsync<Account>(
            "SELECT * FROM Accounts WHERE Id = @Id",
            new { Id = accountId });
    }

    public async Task<Account> CreateAccountAsync(int userId, CreateAccountRequest request)
    {
        var sql = @"
            INSERT INTO Accounts (UserId, Name, Type, Balance, CreditLimit, Currency, Institution, AccountNumber, Color, Icon)
            OUTPUT INSERTED.*
            VALUES (@UserId, @Name, @Type, @Balance, @CreditLimit, @Currency, @Institution, @AccountNumber, @Color, @Icon)";

        var account = await _db.QueryFirstOrDefaultAsync<Account>(sql, new
        {
            UserId = userId,
            request.Name,
            Type = request.Type.ToString(),
            Balance = request.InitialBalance,
            request.CreditLimit,
            request.Currency,
            request.Institution,
            request.AccountNumber,
            request.Color,
            request.Icon
        });

        return account ?? throw new Exception("Failed to create account.");
    }

    public async Task<Account?> UpdateAccountAsync(int accountId, int userId, UpdateAccountRequest request)
    {
        var account = await _db.QueryFirstOrDefaultAsync<Account>(
            "SELECT * FROM Accounts WHERE Id = @Id AND UserId = @UserId",
            new { Id = accountId, UserId = userId });

        if (account == null) return null;

        var sql = @"
            UPDATE Accounts
            SET Name = @Name,
                Institution = @Institution,
                Color = @Color,
                Icon = @Icon,
                IsActive = @IsActive,
                UpdatedAt = GETUTCDATE()
            OUTPUT INSERTED.*
            WHERE Id = @Id AND UserId = @UserId";

        return await _db.QueryFirstOrDefaultAsync<Account>(sql, new
        {
            Id = accountId,
            UserId = userId,
            Name = request.Name ?? account.Name,
            Institution = request.Institution ?? account.Institution,
            Color = request.Color ?? account.Color,
            Icon = request.Icon ?? account.Icon,
            IsActive = request.IsActive ?? account.IsActive
        });
    }

    public async Task<bool> DeleteAccountAsync(int accountId, int userId)
    {
        var affected = await _db.ExecuteAsync(
            "UPDATE Accounts SET IsActive = 0, UpdatedAt = GETUTCDATE() WHERE Id = @Id AND UserId = @UserId",
            new { Id = accountId, UserId = userId });

        return affected > 0;
    }

    public async Task<decimal> GetTotalBalanceAsync(int userId)
    {
        var result = await _db.ExecuteScalarAsync<decimal>(
            "SELECT ISNULL(SUM(Balance), 0) FROM Accounts WHERE UserId = @UserId AND IsActive = 1",
            new { UserId = userId });

        return result;
    }
}
