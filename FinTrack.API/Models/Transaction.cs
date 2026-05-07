namespace FinTrack.API.Models;

public enum TransactionType
{
    Income,
    Expense,
    Transfer
}

public class Transaction
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int AccountId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? SubCategory { get; set; }
    public DateTime TransactionDate { get; set; }
    public string? Notes { get; set; }
    public string? Tags { get; set; }
    public bool IsRecurring { get; set; }
    public string? RecurrencePattern { get; set; }
    public string? MerchantName { get; set; }
    public string? ReferenceNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class CreateTransactionRequest
{
    public int AccountId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? SubCategory { get; set; }
    public DateTime TransactionDate { get; set; }
    public string? Notes { get; set; }
    public string? Tags { get; set; }
    public bool IsRecurring { get; set; }
    public string? RecurrencePattern { get; set; }
    public string? MerchantName { get; set; }
    public string? ReferenceNumber { get; set; }
}

public class UpdateTransactionRequest
{
    public string? Description { get; set; }
    public decimal? Amount { get; set; }
    public string? Category { get; set; }
    public string? SubCategory { get; set; }
    public DateTime? TransactionDate { get; set; }
    public string? Notes { get; set; }
    public string? Tags { get; set; }
    public string? MerchantName { get; set; }
}

public class TransactionFilter
{
    public int? AccountId { get; set; }
    public string? Category { get; set; }
    public TransactionType? Type { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "TransactionDate";
    public string SortOrder { get; set; } = "desc";
}

public class PagedResult<T>
{
    public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
}
