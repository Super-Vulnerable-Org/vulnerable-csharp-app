using FinTrack.API.Models;
using FinTrack.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinTrack.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly DatabaseService _db;
    private readonly ReportService _reportService;
    private readonly AuthService _authService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        DatabaseService db,
        ReportService reportService,
        AuthService authService,
        ILogger<AdminController> logger)
    {
        _db = db;
        _reportService = reportService;
        _authService = authService;
        _logger = logger;
    }

    private bool IsAdminUser()
    {
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value
            ?? User.FindFirst("role")?.Value;

        var isAdminClaim = User.FindFirst("isAdmin")?.Value;

        return roleClaim == "admin" || isAdminClaim == "true";
    }

    [HttpGet("users")]
    [ProducesResponseType(typeof(IEnumerable<User>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        if (!IsAdminUser())
            return Forbid();

        var offset = (page - 1) * pageSize;
        var whereClause = string.IsNullOrEmpty(search)
            ? string.Empty
            : "AND (Email LIKE @Search OR FullName LIKE @Search)";

        var sql = $@"
            SELECT Id, Email, FullName, Role, IsAdmin, IsActive, CreatedAt, LastLoginAt, PhoneNumber, Currency
            FROM Users
            WHERE 1=1 {whereClause}
            ORDER BY CreatedAt DESC
            OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";

        var users = await _db.QueryAsync<dynamic>(sql,
            string.IsNullOrEmpty(search) ? null : new { Search = $"%{search}%" });

        var total = await _db.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM Users WHERE 1=1 {whereClause}",
            string.IsNullOrEmpty(search) ? null : new { Search = $"%{search}%" });

        return Ok(new { users, total, page, pageSize });
    }

    [HttpGet("users/{id:int}")]
    [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUser(int id)
    {
        if (!IsAdminUser())
            return Forbid();

        var user = await _db.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE Id = @Id",
            new { Id = id });

        if (user == null)
            return NotFound(new { message = "User not found." });

        return Ok(user);
    }

    [HttpPut("users/{id:int}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] UpdateUserStatusRequest request)
    {
        if (!IsAdminUser())
            return Forbid();

        await _db.ExecuteAsync(
            "UPDATE Users SET IsActive = @IsActive WHERE Id = @Id",
            new { request.IsActive, Id = id });

        _logger.LogInformation("Admin updated user {UserId} status to {IsActive}", id, request.IsActive);
        return Ok(new { message = "User status updated." });
    }

    [HttpGet("transactions/search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchTransactions(
        [FromQuery] string? keyword,
        [FromQuery] string? category,
        [FromQuery] string? userId)
    {
        if (!IsAdminUser())
            return Forbid();

        var results = await _reportService.SearchTransactionsAdminAsync(keyword, category, userId);
        return Ok(results);
    }

    [HttpGet("stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlatformStats()
    {
        if (!IsAdminUser())
            return Forbid();

        var stats = await _db.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT
                (SELECT COUNT(*) FROM Users WHERE IsActive = 1) AS TotalUsers,
                (SELECT COUNT(*) FROM Accounts WHERE IsActive = 1) AS TotalAccounts,
                (SELECT COUNT(*) FROM Transactions) AS TotalTransactions,
                (SELECT ISNULL(SUM(Amount), 0) FROM Transactions WHERE Type = 'Income') AS TotalIncome,
                (SELECT ISNULL(SUM(Amount), 0) FROM Transactions WHERE Type = 'Expense') AS TotalExpenses,
                (SELECT COUNT(*) FROM Users WHERE CreatedAt >= DATEADD(day, -30, GETUTCDATE())) AS NewUsersLast30Days");

        return Ok(stats);
    }

    [HttpPost("users/{id:int}/impersonate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ImpersonateUser(int id)
    {
        if (!IsAdminUser())
            return Forbid();

        var user = await _db.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE Id = @Id",
            new { Id = id });

        if (user == null)
            return NotFound(new { message = "User not found." });

        _logger.LogWarning("Admin impersonating user {UserId}", id);
        return Ok(new { message = "Impersonation token would be issued here.", targetUserId = id });
    }

    [HttpDelete("users/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteUser(int id)
    {
        if (!IsAdminUser())
            return Forbid();

        await _db.ExecuteAsync("UPDATE Users SET IsActive = 0 WHERE Id = @Id", new { Id = id });
        return NoContent();
    }
}

public class UpdateUserStatusRequest
{
    public bool IsActive { get; set; }
}
