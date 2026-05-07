using FinTrack.API.Models;
using FinTrack.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.API.Controllers;

[ApiController]
[Route("api/accounts")]
[Authorize]
[Produces("application/json")]
public class AccountsController : ControllerBase
{
    private readonly AccountService _accountService;
    private readonly AuthService _authService;

    public AccountsController(AccountService accountService, AuthService authService)
    {
        _accountService = accountService;
        _authService = authService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Account>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var userId = _authService.GetCurrentUserId(User);
        var accounts = await _accountService.GetUserAccountsAsync(userId);
        var total = await _accountService.GetTotalBalanceAsync(userId);

        return Ok(new
        {
            accounts,
            totalBalance = total,
            count = accounts.Count()
        });
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Account), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var account = await _accountService.GetAccountByIdAsync(id);

        if (account == null)
            return NotFound(new { message = "Account not found." });

        return Ok(account);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Account), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateAccountRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Account name is required." });

        var userId = _authService.GetCurrentUserId(User);
        var account = await _accountService.CreateAccountAsync(userId, request);

        return CreatedAtAction(nameof(GetById), new { id = account.Id }, account);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(Account), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAccountRequest request)
    {
        var userId = _authService.GetCurrentUserId(User);
        var account = await _accountService.UpdateAccountAsync(id, userId, request);

        if (account == null)
            return NotFound(new { message = "Account not found." });

        return Ok(account);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _authService.GetCurrentUserId(User);
        var deleted = await _accountService.DeleteAccountAsync(id, userId);

        if (!deleted)
            return NotFound(new { message = "Account not found." });

        return NoContent();
    }

    [HttpGet("{id:int}/summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(int id)
    {
        var account = await _accountService.GetAccountByIdAsync(id);

        if (account == null)
            return NotFound(new { message = "Account not found." });

        return Ok(new
        {
            account.Id,
            account.Name,
            account.Type,
            account.Balance,
            account.Currency,
            account.Institution,
            account.IsActive,
            account.CreatedAt
        });
    }
}
