namespace MainLedger.Contracts.FinancialRecords;

/// <summary>
/// Financial record statistics DTO.
/// </summary>
public class FinancialRecordStatisticsDto
{
    public int TotalRecords { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "USD";
    public decimal AverageAmount { get; init; }
    public Dictionary<string, int> ByType { get; init; } = new();
    public Dictionary<string, int> ByBank { get; init; } = new();
    public List<MonthlyTrendDto> MonthlyTrend { get; init; } = new();
}

public class MonthlyTrendDto
{
    public string Month { get; init; } = string.Empty;
    public int Count { get; init; }
    public decimal Total { get; init; }
}
