namespace FinTrack.API.Models;

public enum AccountType
{
    Checking,
    Savings,
    CreditCard,
    Investment,
    Loan,
    Cash
}

public class Account
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public decimal Balance { get; set; }
    public decimal CreditLimit { get; set; }
    public string Currency { get; set; } = "USD";
    public string? Institution { get; set; }
    public string? AccountNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class CreateAccountRequest
{
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public decimal InitialBalance { get; set; }
    public decimal CreditLimit { get; set; }
    public string Currency { get; set; } = "USD";
    public string? Institution { get; set; }
    public string? AccountNumber { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
}

public class UpdateAccountRequest
{
    public string? Name { get; set; }
    public string? Institution { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public bool? IsActive { get; set; }
}

public class AccountSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? Institution { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
}
