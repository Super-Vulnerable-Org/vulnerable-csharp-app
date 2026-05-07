using FinTrack.API.Models;
using FinTrack.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.API.Controllers;

[ApiController]
[Route("api/budgets")]
[Authorize]
[Produces("application/json")]
public class BudgetsController : ControllerBase
{
    private readonly BudgetService _budgetService;
    private readonly AuthService _authService;

    public BudgetsController(BudgetService budgetService, AuthService authService)
    {
        _budgetService = budgetService;
        _authService = authService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Budget>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var userId = _authService.GetCurrentUserId(User);
        var budgets = await _budgetService.GetBudgetsAsync(userId);
        return Ok(budgets);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Budget), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = _authService.GetCurrentUserId(User);
        var budget = await _budgetService.GetBudgetByIdAsync(id, userId);

        if (budget == null)
            return NotFound(new { message = "Budget not found." });

        return Ok(budget);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Budget), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateBudgetRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Budget name is required." });

        if (request.Amount <= 0)
            return BadRequest(new { message = "Budget amount must be greater than zero." });

        if (request.EndDate <= request.StartDate)
            return BadRequest(new { message = "End date must be after start date." });

        var userId = _authService.GetCurrentUserId(User);
        var budget = await _budgetService.CreateBudgetAsync(userId, request);

        return CreatedAtAction(nameof(GetById), new { id = budget.Id }, budget);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(Budget), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateBudgetRequest request)
    {
        var userId = _authService.GetCurrentUserId(User);
        var budget = await _budgetService.UpdateBudgetAsync(id, userId, request);

        if (budget == null)
            return NotFound(new { message = "Budget not found." });

        return Ok(budget);
    }

    [HttpGet("progress")]
    [ProducesResponseType(typeof(IEnumerable<BudgetProgress>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProgress()
    {
        var userId = _authService.GetCurrentUserId(User);
        var progress = await _budgetService.GetBudgetProgressAsync(userId);
        return Ok(progress);
    }
}
