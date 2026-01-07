namespace MainLedger.Contracts.Emails;

/// <summary>
/// Email list item DTO for paginated email lists.
/// </summary>
public class EmailListItemDto
{
    public Guid Id { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string From { get; init; } = string.Empty;
    public DateTime ReceivedAt { get; init; }
    public string ProcessingStatus { get; init; } = string.Empty;
    public bool? IsFinancial { get; init; }
    public string? Category { get; init; }
    public double? ClassificationConfidence { get; init; }
    public bool HasExtractionCandidate { get; init; }
}
