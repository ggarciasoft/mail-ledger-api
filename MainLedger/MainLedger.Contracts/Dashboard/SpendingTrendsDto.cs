namespace MainLedger.Contracts.Dashboard;

/// <summary>
/// Spending trends DTO with time-series data.
/// </summary>
public class SpendingTrendsDto
{
    public string Period { get; init; } = string.Empty;
    public List<DailySpendingDto> Data { get; init; } = new();
    public decimal TotalSpent { get; init; }
    public decimal AverageDaily { get; init; }
}

public class DailySpendingDto
{
    public string Date { get; init; } = string.Empty;
    public decimal TotalSpent { get; init; }
    public int TransactionCount { get; init; }
}
