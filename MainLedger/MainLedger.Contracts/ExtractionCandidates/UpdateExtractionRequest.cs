namespace MainLedger.Contracts.ExtractionCandidates;

/// <summary>
/// Request to update an extraction candidate before confirmation.
/// </summary>
public class UpdateExtractionRequest
{
    public decimal? Amount { get; init; }
    public string? Currency { get; init; }
    public string? Merchant { get; init; }
    public DateTime? TransactionDate { get; init; }
    public string? SourceAccount { get; init; }
    public string? TargetAccount { get; init; }
    public string? SourceBank { get; init; }
    public string? TargetBank { get; init; }
    public decimal? Fees { get; init; }
    public decimal? Tax { get; init; }
    public string? ReferenceId { get; init; }
}
