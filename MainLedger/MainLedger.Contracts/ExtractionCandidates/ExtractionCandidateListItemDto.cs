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
    public string EmailMessageId { get; init; } = string.Empty;

    // Core transaction data
    public decimal? Amount { get; init; }
    public string? Currency { get; init; }
    public string? Merchant { get; init; }
    public string? MerchantOriginal { get; init; }
    public DateTime? TransactionDate { get; init; }

    // Account information
    public string? SourceAccount { get; init; }
    public string? TargetAccount { get; init; }
    public string? SourceBank { get; init; }
    public string? TargetBank { get; init; }

    // Additional details
    public decimal? Fees { get; init; }
    public decimal? Tax { get; init; }
    public string? ReferenceId { get; init; }

    // Confidence scores
    public double? Confidence { get; init; } // Overall confidence
    public double? AmountConfidence { get; init; }
    public double? DateConfidence { get; init; }
    public double? MerchantConfidence { get; init; }

    // Status
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? ConfirmedAt { get; init; }
    public string? RejectionReason { get; init; }
}
