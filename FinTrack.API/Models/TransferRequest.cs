namespace FinTrack.API.Models;

public class TransferRequest
{
    public int SourceAccountId { get; set; }
    public int DestinationAccountId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateTime TransferDate { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
}

public class TransferResponse
{
    public int Id { get; set; }
    public int SourceAccountId { get; set; }
    public string SourceAccountName { get; set; } = string.Empty;
    public int DestinationAccountId { get; set; }
    public string DestinationAccountName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateTime TransferDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = "Completed";
}
