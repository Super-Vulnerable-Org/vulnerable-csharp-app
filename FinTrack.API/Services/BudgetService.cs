using FinTrack.API.Models;

namespace FinTrack.API.Services;

public class BudgetService
{
    private readonly DatabaseService _db;

    public BudgetService(DatabaseService db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Budget>> GetBudgetsAsync(int userId)
    {
        return await _db.QueryAsync<Budget>(
            "SELECT * FROM Budgets WHERE UserId = @UserId ORDER BY Category",
            new { UserId = userId });
    }

    public async Task<Budget?> GetBudgetByIdAsync(int budgetId, int userId)
    {
        return await _db.QueryFirstOrDefaultAsync<Budget>(
            "SELECT * FROM Budgets WHERE Id = @Id AND UserId = @UserId",
            new { Id = budgetId, UserId = userId });
    }

    public async Task<Budget> CreateBudgetAsync(int userId, CreateBudgetRequest request)
    {
        var sql = @"
            INSERT INTO Budgets (UserId, Name, Category, Amount, Period, StartDate, EndDate, AlertEnabled, AlertThresholdPercent, Color, Notes)
            OUTPUT INSERTED.*
            VALUES (@UserId, @Name, @Category, @Amount, @Period, @StartDate, @EndDate, @AlertEnabled, @AlertThresholdPercent, @Color, @Notes)";

        var budget = await _db.QueryFirstOrDefaultAsync<Budget>(sql, new
        {
            UserId = userId,
            request.Name,
            request.Category,
            request.Amount,
            Period = request.Period.ToString(),
            request.StartDate,
            request.EndDate,
            request.AlertEnabled,
            request.AlertThresholdPercent,
            request.Color,
            request.Notes
        });

        return budget ?? throw new Exception("Failed to create budget.");
    }

    public async Task<Budget?> UpdateBudgetAsync(int budgetId, int userId, UpdateBudgetRequest request)
    {
        var budget = await GetBudgetByIdAsync(budgetId, userId);
        if (budget == null) return null;

        var sql = @"
            UPDATE Budgets
            SET Name = @Name,
                Amount = @Amount,
                AlertEnabled = @AlertEnabled,
                AlertThresholdPercent = @AlertThresholdPercent,
                Color = @Color,
                Notes = @Notes,
                IsActive = @IsActive,
                UpdatedAt = GETUTCDATE()
            OUTPUT INSERTED.*
            WHERE Id = @Id AND UserId = @UserId";

        return await _db.QueryFirstOrDefaultAsync<Budget>(sql, new
        {
            Id = budgetId,
            UserId = userId,
            Name = request.Name ?? budget.Name,
            Amount = request.Amount ?? budget.Amount,
            AlertEnabled = request.AlertEnabled ?? budget.AlertEnabled,
            AlertThresholdPercent = request.AlertThresholdPercent ?? budget.AlertThresholdPercent,
            Color = request.Color ?? budget.Color,
            Notes = request.Notes ?? budget.Notes,
            IsActive = request.IsActive ?? budget.IsActive
        });
    }

    public async Task<IEnumerable<BudgetProgress>> GetBudgetProgressAsync(int userId)
    {
        var sql = @"
            SELECT b.Id, b.Name, b.Category, b.Amount AS BudgetAmount,
                   ISNULL(SUM(t.Amount), 0) AS SpentAmount,
                   b.Period, b.StartDate, b.EndDate, b.Color
            FROM Budgets b
            LEFT JOIN Transactions t ON t.UserId = b.UserId
                AND t.Category = b.Category
                AND t.Type = 'Expense'
                AND t.TransactionDate BETWEEN b.StartDate AND b.EndDate
            WHERE b.UserId = @UserId AND b.IsActive = 1
            GROUP BY b.Id, b.Name, b.Category, b.Amount, b.Period, b.StartDate, b.EndDate, b.Color";

        return await _db.QueryAsync<BudgetProgress>(sql, new { UserId = userId });
    }
}
