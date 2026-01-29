namespace MainLedger.Contracts.Emails;

/// <summary>
/// Detailed email DTO for single email view.
/// </summary>
public class EmailDto
{
    public Guid Id { get; init; }
    public string MessageId { get; init; } = string.Empty;
    public string ThreadId { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty; // "Gmail" or "Outlook"
    public string Subject { get; init; } = string.Empty;
    public string From { get; init; } = string.Empty;
    public DateTime ReceivedAt { get; init; }
    public string BodyText { get; init; } = string.Empty;
    public string ProcessingStatus { get; init; } = string.Empty;
    public string? ProcessingError { get; init; }
    public string? Directive { get; init; }
    public string? DirectiveReason { get; init; }
    public bool? IsFinancial { get; init; }
    public string? Category { get; init; }
    public double? ClassificationConfidence { get; init; }
    public DateTime? ClassifiedAt { get; init; }
    public DateTime CreatedAt { get; init; }
}
