namespace MainLedger.Contracts.ExtractionCandidates;

/// <summary>
/// Extraction candidate list item DTO for paginated lists.
/// </summary>
public class ExtractionCandidateListItemDto
{
    public Guid Id { get; init; }
    public Guid EmailId { get; init; }
    public string EmailSubject { get; init; } = string.Empty;
    public string EmailFrom { get; init; } = string.Empty;
    public DateTime EmailReceivedAt { get; init; }
    public decimal? Amount { get; init; }
    public string? Currency { get; init; }
    public string? Merchant { get; init; }
    public DateTime? TransactionDate { get; init; }
    public string? SourceAccount { get; init; }
    public string? SourceBank { get; init; }
    public string Status { get; init; } = string.Empty;
    public double? Confidence { get; init; }
    public DateTime CreatedAt { get; init; }
}
