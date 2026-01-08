namespace MainLedger.Domain.Models;

/// <summary>
/// Financial record statistics model.
/// </summary>
public class FinancialRecordStatistics
{
    public int TotalRecords { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal AverageAmount { get; init; }
    public Dictionary<string, int> ByType { get; init; } = new();
    public Dictionary<string, int> ByBank { get; init; } = new();
    public List<MonthlyTrend> MonthlyTrend { get; init; } = new();
}

public class MonthlyTrend
{
    public string Month { get; init; } = string.Empty;
    public int Count { get; init; }
    public decimal Total { get; init; }
}
