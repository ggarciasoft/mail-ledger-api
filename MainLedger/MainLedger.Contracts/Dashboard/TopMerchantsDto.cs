namespace MainLedger.Contracts.Dashboard;

/// <summary>
/// Top merchants DTO with spending statistics.
/// </summary>
public class TopMerchantsDto
{
    public List<MerchantStatsDto> Merchants { get; init; } = new();
}

public class MerchantStatsDto
{
    public string Name { get; init; } = string.Empty;
    public decimal TotalSpent { get; init; }
    public int TransactionCount { get; init; }
    public double Percentage { get; init; }
}
