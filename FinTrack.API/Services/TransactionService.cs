using FinTrack.API.Models;

namespace FinTrack.API.Services;

public class TransactionService
{
    private readonly DatabaseService _db;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(DatabaseService db, ILogger<TransactionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Transaction?> GetTransactionByIdAsync(int transactionId)
    {
        return await _db.QueryFirstOrDefaultAsync<Transaction>(
            "SELECT * FROM Transactions WHERE Id = @Id",
            new { Id = transactionId });
    }

    public async Task<PagedResult<Transaction>> GetTransactionsAsync(int userId, TransactionFilter filter)
    {
        var conditions = new List<string> { "UserId = @UserId" };
        var parameters = new Dictionary<string, object> { ["UserId"] = userId };

        if (filter.AccountId.HasValue)
        {
            conditions.Add("AccountId = @AccountId");
            parameters["AccountId"] = filter.AccountId.Value;
        }

        if (filter.Type.HasValue)
        {
            conditions.Add("Type = @Type");
            parameters["Type"] = filter.Type.Value.ToString();
        }

        if (!string.IsNullOrEmpty(filter.Category))
        {
            conditions.Add("Category = @Category");
            parameters["Category"] = filter.Category;
        }

        if (filter.StartDate.HasValue)
        {
            conditions.Add("TransactionDate >= @StartDate");
            parameters["StartDate"] = filter.StartDate.Value;
        }

        if (filter.EndDate.HasValue)
        {
            conditions.Add("TransactionDate <= @EndDate");
            parameters["EndDate"] = filter.EndDate.Value;
        }

        if (filter.MinAmount.HasValue)
        {
            conditions.Add("Amount >= @MinAmount");
            parameters["MinAmount"] = filter.MinAmount.Value;
        }

        if (filter.MaxAmount.HasValue)
        {
            conditions.Add("Amount <= @MaxAmount");
            parameters["MaxAmount"] = filter.MaxAmount.Value;
        }

        if (!string.IsNullOrEmpty(filter.Search))
        {
            conditions.Add("(Description LIKE @Search OR MerchantName LIKE @Search OR Notes LIKE @Search)");
            parameters["Search"] = $"%{filter.Search}%";
        }

        var where = string.Join(" AND ", conditions);
        var sortColumn = filter.SortBy switch
        {
            "Amount" => "Amount",
            "Category" => "Category",
            "Description" => "Description",
            _ => "TransactionDate"
        };
        var sortDir = filter.SortOrder?.ToLower() == "asc" ? "ASC" : "DESC";

        var offset = (filter.Page - 1) * filter.PageSize;

        var countSql = $"SELECT COUNT(*) FROM Transactions WHERE {where}";
        var dataSql = $@"
            SELECT * FROM Transactions
            WHERE {where}
            ORDER BY {sortColumn} {sortDir}
            OFFSET {offset} ROWS FETCH NEXT {filter.PageSize} ROWS ONLY";

        var total = await _db.ExecuteScalarAsync<int>(countSql, parameters);
        var data = await _db.QueryAsync<Transaction>(dataSql, parameters);

        return new PagedResult<Transaction>
        {
            Data = data,
            Total = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<Transaction> CreateTransactionAsync(int userId, CreateTransactionRequest request)
    {
        var account = await _db.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT Id, UserId FROM Accounts WHERE Id = @Id AND UserId = @UserId AND IsActive = 1",
            new { Id = request.AccountId, UserId = userId });

        if (account == null)
            throw new ArgumentException("Account not found or does not belong to user.");

        var sql = @"
            INSERT INTO Transactions
                (UserId, AccountId, Description, Amount, Type, Category, SubCategory,
                 TransactionDate, Notes, Tags, IsRecurring, RecurrencePattern, MerchantName, ReferenceNumber)
            OUTPUT INSERTED.*
            VALUES
                (@UserId, @AccountId, @Description, @Amount, @Type, @Category, @SubCategory,
                 @TransactionDate, @Notes, @Tags, @IsRecurring, @RecurrencePattern, @MerchantName, @ReferenceNumber)";

        var transaction = await _db.QueryFirstOrDefaultAsync<Transaction>(sql, new
        {
            UserId = userId,
            request.AccountId,
            request.Description,
            request.Amount,
            Type = request.Type.ToString(),
            request.Category,
            request.SubCategory,
            request.TransactionDate,
            request.Notes,
            request.Tags,
            request.IsRecurring,
            request.RecurrencePattern,
            request.MerchantName,
            request.ReferenceNumber
        });

        if (transaction == null)
            throw new Exception("Failed to create transaction.");

        var balanceDelta = request.Type == TransactionType.Income ? request.Amount : -request.Amount;
        await _db.ExecuteAsync(
            "UPDATE Accounts SET Balance = Balance + @Delta, UpdatedAt = GETUTCDATE() WHERE Id = @Id",
            new { Delta = balanceDelta, Id = request.AccountId });

        return transaction;
    }

    public async Task<Transaction?> UpdateTransactionAsync(int transactionId, int userId, UpdateTransactionRequest request)
    {
        var existing = await _db.QueryFirstOrDefaultAsync<Transaction>(
            "SELECT * FROM Transactions WHERE Id = @Id AND UserId = @UserId",
            new { Id = transactionId, UserId = userId });

        if (existing == null) return null;

        var sql = @"
            UPDATE Transactions
            SET Description = @Description,
                Amount = @Amount,
                Category = @Category,
                SubCategory = @SubCategory,
                TransactionDate = @TransactionDate,
                Notes = @Notes,
                Tags = @Tags,
                MerchantName = @MerchantName,
                UpdatedAt = GETUTCDATE()
            OUTPUT INSERTED.*
            WHERE Id = @Id AND UserId = @UserId";

        return await _db.QueryFirstOrDefaultAsync<Transaction>(sql, new
        {
            Id = transactionId,
            UserId = userId,
            Description = request.Description ?? existing.Description,
            Amount = request.Amount ?? existing.Amount,
            Category = request.Category ?? existing.Category,
            SubCategory = request.SubCategory ?? existing.SubCategory,
            TransactionDate = request.TransactionDate ?? existing.TransactionDate,
            Notes = request.Notes ?? existing.Notes,
            Tags = request.Tags ?? existing.Tags,
            MerchantName = request.MerchantName ?? existing.MerchantName
        });
    }

    public async Task<bool> DeleteTransactionAsync(int transactionId, int userId)
    {
        var tx = await _db.QueryFirstOrDefaultAsync<Transaction>(
            "SELECT * FROM Transactions WHERE Id = @Id AND UserId = @UserId",
            new { Id = transactionId, UserId = userId });

        if (tx == null) return false;

        await _db.ExecuteAsync(
            "DELETE FROM Transactions WHERE Id = @Id AND UserId = @UserId",
            new { Id = transactionId, UserId = userId });

        var balanceDelta = tx.Type == TransactionType.Income ? -tx.Amount : tx.Amount;
        await _db.ExecuteAsync(
            "UPDATE Accounts SET Balance = Balance + @Delta, UpdatedAt = GETUTCDATE() WHERE Id = @Id",
            new { Delta = balanceDelta, Id = tx.AccountId });

        return true;
    }

    public async Task<Dictionary<string, decimal>> GetCategoryBreakdownAsync(int userId, DateTime from, DateTime to)
    {
        var rows = await _db.QueryAsync<dynamic>(
            @"SELECT Category, SUM(Amount) as Total
              FROM Transactions
              WHERE UserId = @UserId AND Type = 'Expense'
                AND TransactionDate BETWEEN @From AND @To
              GROUP BY Category
              ORDER BY Total DESC",
            new { UserId = userId, From = from, To = to });

        return rows.ToDictionary(
            r => (string)r.Category,
            r => (decimal)r.Total);
    }
}
