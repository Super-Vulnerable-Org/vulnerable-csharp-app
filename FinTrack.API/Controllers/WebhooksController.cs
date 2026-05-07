using FinTrack.API.Models;
using FinTrack.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace FinTrack.API.Controllers;

[ApiController]
[Route("api/webhooks")]
[Authorize]
[Produces("application/json")]
public class WebhooksController : ControllerBase
{
    private readonly DatabaseService _db;
    private readonly AuthService _authService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        DatabaseService db,
        AuthService authService,
        IHttpClientFactory httpClientFactory,
        ILogger<WebhooksController> logger)
    {
        _db = db;
        _authService = authService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Webhook>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var userId = _authService.GetCurrentUserId(User);
        var webhooks = await _db.QueryAsync<Webhook>(
            "SELECT * FROM Webhooks WHERE UserId = @UserId ORDER BY CreatedAt DESC",
            new { UserId = userId });
        return Ok(webhooks);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Webhook), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = _authService.GetCurrentUserId(User);
        var webhook = await _db.QueryFirstOrDefaultAsync<Webhook>(
            "SELECT * FROM Webhooks WHERE Id = @Id AND UserId = @UserId",
            new { Id = id, UserId = userId });

        if (webhook == null)
            return NotFound(new { message = "Webhook not found." });

        return Ok(webhook);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Webhook), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateWebhookRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            return BadRequest(new { message = "Webhook URL is required." });

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Webhook name is required." });

        var userId = _authService.GetCurrentUserId(User);

        var webhook = await _db.QueryFirstOrDefaultAsync<Webhook>(@"
            INSERT INTO Webhooks (UserId, Name, Url, EventType, Secret, RetryCount)
            OUTPUT INSERTED.*
            VALUES (@UserId, @Name, @Url, @EventType, @Secret, @RetryCount)",
            new
            {
                UserId = userId,
                request.Name,
                request.Url,
                EventType = request.EventType.ToString(),
                request.Secret,
                request.RetryCount
            });

        return CreatedAtAction(nameof(GetById), new { id = webhook!.Id }, webhook);
    }

    [HttpPost("{id:int}/test")]
    [ProducesResponseType(typeof(WebhookTestResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Test(int id)
    {
        var userId = _authService.GetCurrentUserId(User);
        var webhook = await _db.QueryFirstOrDefaultAsync<Webhook>(
            "SELECT * FROM Webhooks WHERE Id = @Id AND UserId = @UserId",
            new { Id = id, UserId = userId });

        if (webhook == null)
            return NotFound(new { message = "Webhook not found." });

        var result = await DeliverWebhookAsync(webhook, new
        {
            eventType = "test",
            timestamp = DateTime.UtcNow,
            message = "FinTrack webhook test event"
        });

        return Ok(result);
    }

    [HttpPost("notify")]
    [ProducesResponseType(typeof(WebhookTestResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> SendNotification([FromBody] NotificationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            return BadRequest(new { message = "URL is required." });

        var sw = Stopwatch.StartNew();
        var client = _httpClientFactory.CreateClient("WebhookClient");

        try
        {
            var payload = JsonSerializer.Serialize(new
            {
                eventType = request.EventType,
                data = request.Data,
                timestamp = DateTime.UtcNow
            });

            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(request.Url, content);
            sw.Stop();

            var responseBody = await response.Content.ReadAsStringAsync();

            return Ok(new WebhookTestResult
            {
                Success = response.IsSuccessStatusCode,
                StatusCode = (int)response.StatusCode,
                ResponseBody = responseBody.Length > 500 ? responseBody[..500] : responseBody,
                ResponseTimeMs = sw.ElapsedMilliseconds
            });
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            return Ok(new WebhookTestResult
            {
                Success = false,
                StatusCode = 0,
                ErrorMessage = ex.Message,
                ResponseTimeMs = sw.ElapsedMilliseconds
            });
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _authService.GetCurrentUserId(User);
        var affected = await _db.ExecuteAsync(
            "DELETE FROM Webhooks WHERE Id = @Id AND UserId = @UserId",
            new { Id = id, UserId = userId });

        if (affected == 0)
            return NotFound(new { message = "Webhook not found." });

        return NoContent();
    }

    private async Task<WebhookTestResult> DeliverWebhookAsync(Webhook webhook, object payload)
    {
        var sw = Stopwatch.StartNew();
        var client = _httpClientFactory.CreateClient("WebhookClient");

        try
        {
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            if (!string.IsNullOrEmpty(webhook.Secret))
                client.DefaultRequestHeaders.Add("X-FinTrack-Signature", ComputeSignature(json, webhook.Secret));

            var response = await client.PostAsync(webhook.Url, content);
            sw.Stop();

            await _db.ExecuteAsync(
                "UPDATE Webhooks SET LastTriggeredAt = @Now, LastDeliverySucceeded = @Success WHERE Id = @Id",
                new { Now = DateTime.UtcNow, Success = response.IsSuccessStatusCode, webhook.Id });

            return new WebhookTestResult
            {
                Success = response.IsSuccessStatusCode,
                StatusCode = (int)response.StatusCode,
                ResponseTimeMs = sw.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Webhook delivery failed for webhook {WebhookId}", webhook.Id);
            return new WebhookTestResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ResponseTimeMs = sw.ElapsedMilliseconds
            };
        }
    }

    private static string ComputeSignature(string payload, string secret)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLower();
    }
}

public class NotificationRequest
{
    public string Url { get; set; } = string.Empty;
    public string? EventType { get; set; }
    public object? Data { get; set; }
}
