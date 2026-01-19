using MainLedger.Domain.Common;
using MainLedger.Domain.Enums;
using MainLedger.Domain.ValueObjects;

namespace MainLedger.Domain.Entities;

/// <summary>
/// Represents extracted financial data from an email (candidate before user confirmation).
/// </summary>
public sealed class ExtractionCandidate : Entity
{
    public Guid EmailMessageId { get; private set; }
    public Guid ExtractionVersionId { get; private set; }

    // Email metadata (denormalized for persistence after email deletion)
    public string EmailSubject { get; private set; } = string.Empty;
    public string EmailFrom { get; private set; } = string.Empty;
    public DateTime EmailReceivedAt { get; private set; }
    public string EmailMessageIdExternal { get; private set; } = string.Empty; // Gmail message ID

    // Core transaction data
    public Money? Amount { get; private set; }
    public DateTime? TransactionDate { get; private set; }
    public string? Merchant { get; private set; } // Normalized merchant name
    public string? MerchantOriginal { get; private set; } // Original AI-extracted merchant name

    // Account information
    public AccountNumber? SourceAccount { get; private set; }
    public AccountNumber? TargetAccount { get; private set; }
    public BankProvider? SourceBank { get; private set; }
    public BankProvider? TargetBank { get; private set; }

    // Additional financial details
    public Money? Fees { get; private set; }
    public Money? Tax { get; private set; }
    public string? ReferenceId { get; private set; }

    // Confidence scores
    public Confidence? AmountConfidence { get; private set; }
    public Confidence? DateConfidence { get; private set; }
    public Confidence? MerchantConfidence { get; private set; }

    // Status tracking
    public RecordStatus Status { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }

    private ExtractionCandidate(
        Guid id,
        Guid emailMessageId,
        Guid extractionVersionId,
        DateTime createdAt
    )
        : base(id)
    {
        EmailMessageId = emailMessageId;
        ExtractionVersionId = extractionVersionId;
        Status = RecordStatus.Pending;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Creates a new extraction candidate.
    /// </summary>
    public static ExtractionCandidate Create(Guid emailMessageId, Guid extractionVersionId)
    {
        return new ExtractionCandidate(
            Guid.NewGuid(),
            emailMessageId,
            extractionVersionId,
            DateTime.UtcNow
        );
    }

    /// <summary>
    /// Sets the email metadata for this candidate.
    /// </summary>
    public void SetEmailMetadata(string subject, string from, DateTime receivedAt, string messageId)
    {
        EmailSubject = subject ?? string.Empty;
        EmailFrom = from ?? string.Empty;
        EmailReceivedAt = receivedAt;
        EmailMessageIdExternal = messageId ?? string.Empty;
    }

    /// <summary>
    /// Sets the core transaction data.
    /// </summary>
    public void SetTransactionData(
        Money? amount,
        DateTime? transactionDate,
        string? merchant,
        string? merchantOriginal,
        Confidence? amountConfidence,
        Confidence? dateConfidence,
        Confidence? merchantConfidence
    )
    {
        Amount = amount;
        TransactionDate = transactionDate;
        Merchant = merchant;
        MerchantOriginal = merchantOriginal;
        AmountConfidence = amountConfidence;
        DateConfidence = dateConfidence;
        MerchantConfidence = merchantConfidence;
    }

    /// <summary>
    /// Sets account information.
    /// </summary>
    public void SetAccountInfo(
        AccountNumber? sourceAccount,
        AccountNumber? targetAccount,
        BankProvider? sourceBank,
        BankProvider? targetBank
    )
    {
        SourceAccount = sourceAccount;
        TargetAccount = targetAccount;
        SourceBank = sourceBank;
        TargetBank = targetBank;
    }

    /// <summary>
    /// Sets additional financial details.
    /// </summary>
    public void SetAdditionalDetails(Money? fees, Money? tax, string? referenceId)
    {
        Fees = fees;
        Tax = tax;
        ReferenceId = referenceId;
    }

    /// <summary>
    /// Confirms the extraction as valid.
    /// </summary>
    public void Confirm()
    {
        if (Status != RecordStatus.Pending)
        {
            throw new InvalidOperationException("Only pending extractions can be confirmed.");
        }

        Status = RecordStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Rejects the extraction.
    /// </summary>
    public void Reject(string reason)
    {
        if (Status != RecordStatus.Pending)
        {
            throw new InvalidOperationException("Only pending extractions can be rejected.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Rejection reason is required.", nameof(reason));
        }

        Status = RecordStatus.Rejected;
        RejectionReason = reason;
    }

    // For EF Core
    private ExtractionCandidate()
        : base() { }
}
