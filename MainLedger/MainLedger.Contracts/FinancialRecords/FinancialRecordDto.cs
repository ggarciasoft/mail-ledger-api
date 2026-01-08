namespace MainLedger.Contracts.FinancialRecords;

/// <summary>
/// Detailed financial record DTO.
/// </summary>
public class FinancialRecordDto
{
    public Guid Id { get; init; }
    public Guid EmailId { get; init; }
    public string Type { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string Direction { get; init; } = string.Empty;
    public string? Merchant { get; init; }
    public string? SourceAccount { get; init; }
    public string? SourceBank { get; init; }
    public string? TargetAccount { get; init; }
    public string? TargetBank { get; init; }
    public DateTime TransactionDate { get; init; }
    public decimal? TaxAmount { get; init; }
    public decimal? FeeAmount { get; init; }
    public double Confidence { get; init; }
    public string ExtractionVersion { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime ConfirmedAt { get; init; }
}
