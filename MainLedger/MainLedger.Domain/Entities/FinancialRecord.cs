using MainLedger.Domain.Common;
using MainLedger.Domain.Enums;
using MainLedger.Domain.ValueObjects;

namespace MainLedger.Domain.Entities;

/// <summary>
/// Represents a confirmed financial transaction extracted from an email.
/// This is the core entity representing the canonical financial event model.
/// Once confirmed, this entity is IMMUTABLE.
/// </summary>
public sealed class FinancialRecord : Entity
{
    public Guid UserId { get; private set; }
    public Guid EmailMessageId { get; private set; }
    public TransactionType Type { get; private set; }
    public Money Amount { get; private set; }
    public TransactionDirection Direction { get; private set; }
    public string? Merchant { get; private set; }
    public AccountNumber? SourceAccount { get; private set; }
    public BankProvider? SourceBank { get; private set; }
    public AccountNumber? TargetAccount { get; private set; }
    public BankProvider? TargetBank { get; private set; }
    public DateTime TransactionDate { get; private set; }
    public Money? TaxAmount { get; private set; }
    public Money? FeeAmount { get; private set; }
    public Confidence Confidence { get; private set; }
    public RecordStatus Status { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? RejectedAt { get; private set; }
    public string ExtractionVersion { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private FinancialRecord(
        Guid id,
        Guid userId,
        Guid emailMessageId,
        TransactionType type,
        Money amount,
        TransactionDirection direction,
        string? merchant,
        AccountNumber? sourceAccount,
        BankProvider? sourceBank,
        AccountNumber? targetAccount,
        BankProvider? targetBank,
        DateTime transactionDate,
        Money? taxAmount,
        Money? feeAmount,
        Confidence confidence,
        string extractionVersion,
        DateTime createdAt) : base(id)
    {
        // Validation
        if (transactionDate > DateTime.UtcNow)
        {
            throw new ArgumentException("Transaction date cannot be in the future.", nameof(transactionDate));
        }

        if (type == TransactionType.Transfer && (sourceAccount == null || targetAccount == null))
        {
            throw new InvalidOperationException(
                "Transfer transactions must have both source and target accounts.");
        }

        if (taxAmount != null && taxAmount.Currency != amount.Currency)
        {
            throw new InvalidOperationException("Tax amount must have the same currency as the transaction amount.");
        }

        if (feeAmount != null && feeAmount.Currency != amount.Currency)
        {
            throw new InvalidOperationException("Fee amount must have the same currency as the transaction amount.");
        }

        if (string.IsNullOrWhiteSpace(extractionVersion))
        {
            throw new ArgumentException("Extraction version cannot be empty.", nameof(extractionVersion));
        }

        UserId = userId;
        EmailMessageId = emailMessageId;
        Type = type;
        Amount = amount ?? throw new ArgumentNullException(nameof(amount));
        Direction = direction;
        Merchant = merchant;
        SourceAccount = sourceAccount;
        SourceBank = sourceBank;
        TargetAccount = targetAccount;
        TargetBank = targetBank;
        TransactionDate = transactionDate;
        TaxAmount = taxAmount;
        FeeAmount = feeAmount;
        Confidence = confidence ?? throw new ArgumentNullException(nameof(confidence));
        Status = RecordStatus.Pending;
        ExtractionVersion = extractionVersion;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Creates a new financial record in Pending status.
    /// </summary>
    public static FinancialRecord Create(
        Guid userId,
        Guid emailMessageId,
        TransactionType type,
        Money amount,
        TransactionDirection direction,
        Confidence confidence,
        string extractionVersion,
        DateTime transactionDate,
        string? merchant = null,
        AccountNumber? sourceAccount = null,
        BankProvider? sourceBank = null,
        AccountNumber? targetAccount = null,
        BankProvider? targetBank = null,
        Money? taxAmount = null,
        Money? feeAmount = null)
    {
        return new FinancialRecord(
            Guid.NewGuid(),
            userId,
            emailMessageId,
            type,
            amount,
            direction,
            merchant,
            sourceAccount,
            sourceBank,
            targetAccount,
            targetBank,
            transactionDate,
            taxAmount,
            feeAmount,
            confidence,
            extractionVersion,
            DateTime.UtcNow);
    }

    /// <summary>
    /// Confirms the financial record, making it immutable.
    /// </summary>
    public void Confirm()
    {
        if (Status == RecordStatus.Confirmed)
        {
            throw new InvalidOperationException("Financial record is already confirmed.");
        }

        if (Status == RecordStatus.Rejected)
        {
            throw new InvalidOperationException("Cannot confirm a rejected financial record.");
        }

        Status = RecordStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Rejects the financial record.
    /// </summary>
    public void Reject()
    {
        if (Status == RecordStatus.Confirmed)
        {
            throw new InvalidOperationException("Cannot reject a confirmed financial record.");
        }

        if (Status == RecordStatus.Rejected)
        {
            throw new InvalidOperationException("Financial record is already rejected.");
        }

        Status = RecordStatus.Rejected;
        RejectedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Returns true if the record can be modified (not confirmed or rejected).
    /// </summary>
    public bool CanBeModified()
    {
        return Status == RecordStatus.Pending;
    }

    /// <summary>
    /// Updates the financial record details. Only allowed if status is Pending.
    /// </summary>
    public void Update(
        TransactionType? type = null,
        Money? amount = null,
        TransactionDirection? direction = null,
        string? merchant = null,
        AccountNumber? sourceAccount = null,
        BankProvider? sourceBank = null,
        AccountNumber? targetAccount = null,
        BankProvider? targetBank = null,
        DateTime? transactionDate = null,
        Money? taxAmount = null,
        Money? feeAmount = null)
    {
        if (!CanBeModified())
        {
            throw new InvalidOperationException(
                "Cannot modify a financial record that is confirmed or rejected.");
        }

        if (type.HasValue) Type = type.Value;
        if (amount != null) Amount = amount;
        if (direction.HasValue) Direction = direction.Value;
        if (merchant != null) Merchant = merchant;
        if (sourceAccount != null) SourceAccount = sourceAccount;
        if (sourceBank != null) SourceBank = sourceBank;
        if (targetAccount != null) TargetAccount = targetAccount;
        if (targetBank != null) TargetBank = targetBank;
        if (transactionDate.HasValue)
        {
            if (transactionDate.Value > DateTime.UtcNow)
            {
                throw new ArgumentException("Transaction date cannot be in the future.");
            }
            TransactionDate = transactionDate.Value;
        }
        if (taxAmount != null) TaxAmount = taxAmount;
        if (feeAmount != null) FeeAmount = feeAmount;

        // Validate transfer invariant
        if (Type == TransactionType.Transfer && (SourceAccount == null || TargetAccount == null))
        {
            throw new InvalidOperationException(
                "Transfer transactions must have both source and target accounts.");
        }
    }

    // For EF Core
    private FinancialRecord() : base() { }
}
