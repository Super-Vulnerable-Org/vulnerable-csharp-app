using FinTrack.API.Models;
using System.Text;

namespace FinTrack.API.Services;

public class ReportService
{
    private readonly DatabaseService _db;
    private readonly IConfiguration _configuration;

    public ReportService(DatabaseService db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public async Task<object> GetMonthlySummaryAsync(int userId, int year, int month)
    {
        var sql = @"
            SELECT
                SUM(CASE WHEN Type = 'Income' THEN Amount ELSE 0 END) AS TotalIncome,
                SUM(CASE WHEN Type = 'Expense' THEN Amount ELSE 0 END) AS TotalExpenses,
                COUNT(*) AS TransactionCount,
                AVG(CASE WHEN Type = 'Expense' THEN Amount END) AS AvgExpense
            FROM Transactions
            WHERE UserId = @UserId
              AND YEAR(TransactionDate) = @Year
              AND MONTH(TransactionDate) = @Month";

        var summary = await _db.QueryFirstOrDefaultAsync<dynamic>(sql, new { UserId = userId, Year = year, Month = month });

        var breakdown = await _db.QueryAsync<dynamic>(@"
            SELECT Category, SUM(Amount) AS Total, COUNT(*) AS Count
            FROM Transactions
            WHERE UserId = @UserId
              AND Type = 'Expense'
              AND YEAR(TransactionDate) = @Year
              AND MONTH(TransactionDate) = @Month
            GROUP BY Category
            ORDER BY Total DESC",
            new { UserId = userId, Year = year, Month = month });

        return new { Summary = summary, CategoryBreakdown = breakdown };
    }

    public async Task<object> GetCashFlowAsync(int userId, DateTime from, DateTime to)
    {
        var sql = @"
            SELECT
                CAST(TransactionDate AS DATE) AS Date,
                SUM(CASE WHEN Type = 'Income' THEN Amount ELSE 0 END) AS Income,
                SUM(CASE WHEN Type = 'Expense' THEN Amount ELSE 0 END) AS Expenses
            FROM Transactions
            WHERE UserId = @UserId
              AND TransactionDate BETWEEN @From AND @To
            GROUP BY CAST(TransactionDate AS DATE)
            ORDER BY Date";

        return await _db.QueryAsync<dynamic>(sql, new { UserId = userId, From = from, To = to });
    }

    public async Task<string> GenerateCsvExportAsync(int userId, DateTime from, DateTime to)
    {
        var transactions = await _db.QueryAsync<Transaction>(@"
            SELECT t.*, a.Name AS AccountName
            FROM Transactions t
            JOIN Accounts a ON a.Id = t.AccountId
            WHERE t.UserId = @UserId
              AND t.TransactionDate BETWEEN @From AND @To
            ORDER BY t.TransactionDate DESC",
            new { UserId = userId, From = from, To = to });

        var sb = new StringBuilder();
        sb.AppendLine("Date,Description,Category,Type,Amount,Account,MerchantName,Notes");

        foreach (var tx in transactions)
        {
            sb.AppendLine(string.Join(",",
                tx.TransactionDate.ToString("yyyy-MM-dd"),
                $"\"{tx.Description?.Replace("\"", "\"\"")}\"",
                tx.Category,
                tx.Type,
                tx.Amount.ToString("F2"),
                $"\"{tx.MerchantName?.Replace("\"", "\"\"")}\"",
                $"\"{tx.Notes?.Replace("\"", "\"\"")}\""));
        }

        return sb.ToString();
    }

    public async Task SaveExportFileAsync(int userId, string content, string filename)
    {
        var exportsDir = _configuration["App:ExportsDirectory"] ?? "/var/fintrack/exports";
        var userDir = Path.Combine(exportsDir, userId.ToString());
        Directory.CreateDirectory(userDir);

        var filePath = Path.Combine(userDir, filename);
        await File.WriteAllTextAsync(filePath, content);
    }

    public async Task<IEnumerable<dynamic>> SearchTransactionsAdminAsync(string? keyword, string? category, string? userId)
    {
        var where = "1=1";

        if (!string.IsNullOrEmpty(keyword))
            where += $" AND (t.Description LIKE '%{keyword}%' OR t.MerchantName LIKE '%{keyword}%')";

        if (!string.IsNullOrEmpty(category))
            where += $" AND t.Category = '{category}'";

        if (!string.IsNullOrEmpty(userId))
            where += $" AND t.UserId = {userId}";

        var sql = $@"
            SELECT t.*, u.Email, u.FullName, a.Name AS AccountName
            FROM Transactions t
            JOIN Users u ON u.Id = t.UserId
            JOIN Accounts a ON a.Id = t.AccountId
            WHERE {where}
            ORDER BY t.TransactionDate DESC";

        return await _db.QueryRawAsync<dynamic>(sql);
    }
}
