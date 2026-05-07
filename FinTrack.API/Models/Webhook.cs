namespace FinTrack.API.Models;

public enum WebhookEventType
{
    TransactionCreated,
    BudgetAlert,
    LowBalance,
    PaymentDue,
    MonthlyReport
}

public class Webhook
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public WebhookEventType EventType { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Secret { get; set; }
    public int RetryCount { get; set; } = 3;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastTriggeredAt { get; set; }
    public bool? LastDeliverySucceeded { get; set; }
}

public class CreateWebhookRequest
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public WebhookEventType EventType { get; set; }
    public string? Secret { get; set; }
    public int RetryCount { get; set; } = 3;
}

public class WebhookTestResult
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public long ResponseTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
}
