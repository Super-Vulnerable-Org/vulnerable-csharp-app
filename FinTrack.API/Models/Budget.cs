namespace FinTrack.API.Models;

public enum BudgetPeriod
{
    Weekly,
    Monthly,
    Quarterly,
    Yearly
}

public class Budget
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal SpentAmount { get; set; }
    public BudgetPeriod Period { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public bool AlertEnabled { get; set; } = true;
    public int AlertThresholdPercent { get; set; } = 80;
    public string? Color { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class CreateBudgetRequest
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public BudgetPeriod Period { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool AlertEnabled { get; set; } = true;
    public int AlertThresholdPercent { get; set; } = 80;
    public string? Color { get; set; }
    public string? Notes { get; set; }
}

public class UpdateBudgetRequest
{
    public string? Name { get; set; }
    public decimal? Amount { get; set; }
    public bool? AlertEnabled { get; set; }
    public int? AlertThresholdPercent { get; set; }
    public string? Color { get; set; }
    public string? Notes { get; set; }
    public bool? IsActive { get; set; }
}

public class BudgetProgress
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal BudgetAmount { get; set; }
    public decimal SpentAmount { get; set; }
    public decimal RemainingAmount => BudgetAmount - SpentAmount;
    public double PercentUsed => BudgetAmount > 0 ? (double)(SpentAmount / BudgetAmount) * 100 : 0;
    public bool IsOverBudget => SpentAmount > BudgetAmount;
    public string Period { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Color { get; set; }
}
