using MainLedger.Domain.Common;
using MainLedger.Domain.Enums;
using MainLedger.Domain.ValueObjects;

namespace MainLedger.Domain.Entities;

/// <summary>
/// Represents an ingested email message from Gmail.
/// Temporary storage for processing pipeline.
/// </summary>
public sealed class EmailMessage : Entity
{
    public string MessageId { get; private set; }
    public string ThreadId { get; private set; }
    public Guid UserId { get; private set; }
    public string Subject { get; private set; }
    public EmailAddress From { get; private set; }
    public DateTime ReceivedAt { get; private set; }
    public string BodyText { get; private set; }
    public string ContentHash { get; private set; }
    public bool IsProcessed { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public ProcessingDirective? Directive { get; private set; }
    public string? DirectiveReason { get; private set; }
    public Guid? MatchedRuleId { get; private set; }
    
    // Classification fields
    public bool? IsFinancial { get; private set; }
    public EmailCategory? Category { get; private set; }
    public Confidence? ClassificationConfidence { get; private set; }
    public DateTime? ClassifiedAt { get; private set; }
    
    // Processing status tracking
    public EmailProcessingStatus ProcessingStatus { get; private set; }
    public string? ProcessingError { get; private set; }
    
    public DateTime CreatedAt { get; private set; }

    public EmailConnection? EmailConnection { get; private set; }

    private EmailMessage(
        Guid id,
        string messageId,
        string threadId,
        Guid userId,
        string subject,
        EmailAddress from,
        DateTime receivedAt,
        string bodyText,
        string contentHash,
        DateTime createdAt) : base(id)
    {
        if (string.IsNullOrWhiteSpace(messageId))
            throw new ArgumentException("Message ID cannot be empty.", nameof(messageId));
        if (string.IsNullOrWhiteSpace(threadId))
            throw new ArgumentException("Thread ID cannot be empty.", nameof(threadId));
        if (string.IsNullOrWhiteSpace(contentHash))
            throw new ArgumentException("Content hash cannot be empty.", nameof(contentHash));

        MessageId = messageId;
        ThreadId = threadId;
        UserId = userId;
        Subject = subject ?? string.Empty;
        From = from ?? throw new ArgumentNullException(nameof(from));
        ReceivedAt = receivedAt;
        BodyText = bodyText ?? string.Empty;
        ContentHash = contentHash;
        IsProcessed = false;
        ProcessingStatus = EmailProcessingStatus.Pending;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Creates a new email message.
    /// </summary>
    public static EmailMessage Create(
        string messageId,
        string threadId,
        Guid userId,
        string subject,
        EmailAddress from,
        DateTime receivedAt,
        string bodyText,
        string contentHash)
    {
        return new EmailMessage(
            Guid.NewGuid(),
            messageId,
            threadId,
            userId,
            subject,
            from,
            receivedAt,
            bodyText,
            contentHash,
            DateTime.UtcNow);
    }

    /// <summary>
    /// Sets the processing directive from rules engine evaluation.
    /// </summary>
    public void SetDirective(RuleEvaluationResult evaluation)
    {
        if (evaluation == null)
            throw new ArgumentNullException(nameof(evaluation));

        Directive = evaluation.Directive;
        DirectiveReason = evaluation.Reason;
        MatchedRuleId = evaluation.MatchedRuleId;
    }

    /// <summary>
    /// Sets the classification result from AI classification.
    /// </summary>
    public void SetClassification(bool isFinancial, EmailCategory? category, Confidence confidence)
    {
        if (confidence == null)
            throw new ArgumentNullException(nameof(confidence));

        IsFinancial = isFinancial;
        Category = category;
        ClassificationConfidence = confidence;
        ClassifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the email as processed.
    /// </summary>
    public void MarkAsProcessed()
    {
        if (IsProcessed)
        {
            throw new InvalidOperationException("Email is already marked as processed.");
        }

        IsProcessed = true;
        ProcessedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the processing status of the email.
    /// </summary>
    public void SetProcessingStatus(EmailProcessingStatus status, string? error = null)
    {
        ProcessingStatus = status;
        ProcessingError = error;

        if (status == EmailProcessingStatus.Extracted)
        {
            MarkAsProcessed();
        }
    }

    // For EF Core
    private EmailMessage() : base() { }
}
