using FinTrack.API.Models;
using FinTrack.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.API.Controllers;

[ApiController]
[Route("api/transactions")]
[Authorize]
[Produces("application/json")]
public class TransactionsController : ControllerBase
{
    private readonly TransactionService _transactionService;
    private readonly AuthService _authService;

    public TransactionsController(TransactionService transactionService, AuthService authService)
    {
        _transactionService = transactionService;
        _authService = authService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<Transaction>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] TransactionFilter filter)
    {
        var userId = _authService.GetCurrentUserId(User);
        var result = await _transactionService.GetTransactionsAsync(userId, filter);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Transaction), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var transaction = await _transactionService.GetTransactionByIdAsync(id);

        if (transaction == null)
            return NotFound(new { message = "Transaction not found." });

        return Ok(transaction);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Transaction), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTransactionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
            return BadRequest(new { message = "Description is required." });

        if (request.Amount <= 0)
            return BadRequest(new { message = "Amount must be greater than zero." });

        var userId = _authService.GetCurrentUserId(User);
        var transaction = await _transactionService.CreateTransactionAsync(userId, request);

        return CreatedAtAction(nameof(GetById), new { id = transaction.Id }, transaction);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(Transaction), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTransactionRequest request)
    {
        var userId = _authService.GetCurrentUserId(User);
        var transaction = await _transactionService.UpdateTransactionAsync(id, userId, request);

        if (transaction == null)
            return NotFound(new { message = "Transaction not found." });

        return Ok(transaction);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _authService.GetCurrentUserId(User);
        var deleted = await _transactionService.DeleteTransactionAsync(id, userId);

        if (!deleted)
            return NotFound(new { message = "Transaction not found." });

        return NoContent();
    }

    [HttpGet("categories")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategoryBreakdown(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var userId = _authService.GetCurrentUserId(User);
        var startDate = from ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var endDate = to ?? DateTime.UtcNow;

        var breakdown = await _transactionService.GetCategoryBreakdownAsync(userId, startDate, endDate);
        return Ok(breakdown);
    }

    [HttpGet("recent")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecent([FromQuery] int limit = 10)
    {
        var userId = _authService.GetCurrentUserId(User);
        var filter = new TransactionFilter
        {
            Page = 1,
            PageSize = Math.Min(limit, 50),
            SortBy = "TransactionDate",
            SortOrder = "desc"
        };
        var result = await _transactionService.GetTransactionsAsync(userId, filter);
        return Ok(result.Data);
    }
}
