using FinTrack.API.Models;
using FinTrack.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.API.Controllers;

[ApiController]
[Route("api/transfers")]
[Authorize]
[Produces("application/json")]
public class TransfersController : ControllerBase
{
    private readonly DatabaseService _db;
    private readonly AuthService _authService;
    private readonly ILogger<TransfersController> _logger;

    public TransfersController(DatabaseService db, AuthService authService, ILogger<TransfersController> logger)
    {
        _db = db;
        _authService = authService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(TransferResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Transfer([FromBody] TransferRequest request)
    {
        if (request.Amount <= 0)
            return BadRequest(new { message = "Transfer amount must be greater than zero." });

        if (request.SourceAccountId == request.DestinationAccountId)
            return BadRequest(new { message = "Source and destination accounts must be different." });

        var userId = _authService.GetCurrentUserId(User);

        var sourceAccount = await _db.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT Id, Name, Balance, UserId FROM Accounts WHERE Id = @Id AND IsActive = 1",
            new { Id = request.SourceAccountId });

        if (sourceAccount == null)
            return BadRequest(new { message = "Source account not found." });

        var destinationAccount = await _db.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT Id, Name, UserId FROM Accounts WHERE Id = @Id AND IsActive = 1",
            new { Id = request.DestinationAccountId });

        if (destinationAccount == null)
            return BadRequest(new { message = "Destination account not found." });

        if ((decimal)sourceAccount.Balance < request.Amount)
            return BadRequest(new { message = "Insufficient funds in source account." });

        await _db.ExecuteAsync(
            "UPDATE Accounts SET Balance = Balance - @Amount, UpdatedAt = GETUTCDATE() WHERE Id = @Id",
            new { request.Amount, Id = request.SourceAccountId });

        await _db.ExecuteAsync(
            "UPDATE Accounts SET Balance = Balance + @Amount, UpdatedAt = GETUTCDATE() WHERE Id = @Id",
            new { request.Amount, Id = request.DestinationAccountId });

        var description = request.Description ?? $"Transfer to {destinationAccount.Name}";

        var debitTx = await _db.QueryFirstOrDefaultAsync<dynamic>(@"
            INSERT INTO Transactions (UserId, AccountId, Description, Amount, Type, Category, TransactionDate, Notes)
            OUTPUT INSERTED.Id
            VALUES (@UserId, @AccountId, @Description, @Amount, 'Transfer', 'Transfer', @TransferDate, @Notes)",
            new
            {
                UserId = userId,
                AccountId = request.SourceAccountId,
                Description = description,
                request.Amount,
                request.TransferDate,
                Notes = request.Notes
            });

        var creditTx = await _db.QueryFirstOrDefaultAsync<dynamic>(@"
            INSERT INTO Transactions (UserId, AccountId, Description, Amount, Type, Category, TransactionDate, Notes)
            OUTPUT INSERTED.Id
            VALUES (@UserId, @AccountId, @Description, @Amount, 'Transfer', 'Transfer', @TransferDate, @Notes)",
            new
            {
                UserId = userId,
                AccountId = request.DestinationAccountId,
                Description = $"Transfer from {sourceAccount.Name}",
                request.Amount,
                request.TransferDate,
                Notes = request.Notes
            });

        var transferRecord = await _db.QueryFirstOrDefaultAsync<dynamic>(@"
            INSERT INTO Transfers (UserId, SourceAccountId, DestinationAccountId, Amount, Description, TransferDate, Notes)
            OUTPUT INSERTED.*
            VALUES (@UserId, @SourceAccountId, @DestinationAccountId, @Amount, @Description, @TransferDate, @Notes)",
            new
            {
                UserId = userId,
                request.SourceAccountId,
                request.DestinationAccountId,
                request.Amount,
                Description = description,
                request.TransferDate,
                request.Notes
            });

        _logger.LogInformation(
            "Transfer of {Amount} from account {Source} to account {Destination} by user {UserId}",
            request.Amount, request.SourceAccountId, request.DestinationAccountId, userId);

        return Ok(new TransferResponse
        {
            Id = (int)transferRecord!.Id,
            SourceAccountId = request.SourceAccountId,
            SourceAccountName = (string)sourceAccount.Name,
            DestinationAccountId = request.DestinationAccountId,
            DestinationAccountName = (string)destinationAccount.Name,
            Amount = request.Amount,
            Description = description,
            TransferDate = request.TransferDate,
            CreatedAt = DateTime.UtcNow,
            Status = "Completed"
        });
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransfers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = _authService.GetCurrentUserId(User);
        var offset = (page - 1) * pageSize;

        var transfers = await _db.QueryAsync<dynamic>(@"
            SELECT t.*,
                   sa.Name AS SourceAccountName,
                   da.Name AS DestinationAccountName
            FROM Transfers t
            JOIN Accounts sa ON sa.Id = t.SourceAccountId
            JOIN Accounts da ON da.Id = t.DestinationAccountId
            WHERE t.UserId = @UserId
            ORDER BY t.CreatedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY",
            new { UserId = userId, Offset = offset, PageSize = pageSize });

        return Ok(transfers);
    }
}
