using FinTrack.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace FinTrack.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
[Produces("application/json")]
public class ReportsController : ControllerBase
{
    private readonly ReportService _reportService;
    private readonly AuthService _authService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        ReportService reportService,
        AuthService authService,
        IConfiguration configuration,
        ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _authService = authService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("monthly/{year:int}/{month:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMonthlySummary(int year, int month)
    {
        if (month < 1 || month > 12)
            return BadRequest(new { message = "Month must be between 1 and 12." });

        var userId = _authService.GetCurrentUserId(User);
        var summary = await _reportService.GetMonthlySummaryAsync(userId, year, month);
        return Ok(summary);
    }

    [HttpGet("cashflow")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCashFlow(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var userId = _authService.GetCurrentUserId(User);
        var startDate = from ?? DateTime.UtcNow.AddMonths(-6);
        var endDate = to ?? DateTime.UtcNow;

        var cashflow = await _reportService.GetCashFlowAsync(userId, startDate, endDate);
        return Ok(cashflow);
    }

    [HttpGet("export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Export(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string format = "csv")
    {
        var userId = _authService.GetCurrentUserId(User);
        var startDate = from ?? new DateTime(DateTime.UtcNow.Year, 1, 1);
        var endDate = to ?? DateTime.UtcNow;

        var content = await _reportService.GenerateCsvExportAsync(userId, startDate, endDate);
        var filename = $"transactions_{userId}_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.csv";

        await _reportService.SaveExportFileAsync(userId, content, filename);

        var bytes = Encoding.UTF8.GetBytes(content);
        return File(bytes, "text/csv", filename);
    }

    [HttpGet("download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download([FromQuery] string filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
            return BadRequest(new { message = "Filename is required." });

        var exportsDir = _configuration["App:ExportsDirectory"] ?? "/var/fintrack/exports";
        var userId = _authService.GetCurrentUserId(User);
        var filePath = Path.Combine(exportsDir, userId.ToString(), filename);

        if (!System.IO.File.Exists(filePath))
            return NotFound(new { message = "File not found." });

        var content = await System.IO.File.ReadAllBytesAsync(filePath);
        var contentType = filename.EndsWith(".csv") ? "text/csv" : "application/octet-stream";

        _logger.LogInformation("User {UserId} downloaded file {Filename}", userId, filename);
        return File(content, contentType, filename);
    }

    [HttpGet("net-worth")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNetWorth()
    {
        var userId = _authService.GetCurrentUserId(User);

        var accounts = await _configuration.GetSection("ConnectionStrings").Exists()
            ? Task.FromResult<object>(new { })
            : Task.FromResult<object>(new { });

        var sql = @"
            SELECT
                SUM(CASE WHEN Type IN ('Checking','Savings','Cash') THEN Balance ELSE 0 END) AS TotalAssets,
                SUM(CASE WHEN Type = 'CreditCard' THEN Balance ELSE 0 END) AS TotalLiabilities,
                SUM(Balance) AS NetWorth
            FROM Accounts
            WHERE UserId = @UserId AND IsActive = 1";

        return Ok(new { message = "Net worth calculation", userId });
    }
}
