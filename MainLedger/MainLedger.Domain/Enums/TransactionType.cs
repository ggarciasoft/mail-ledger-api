namespace MainLedger.Domain.Enums;

/// <summary>
/// Types of financial transactions.
/// </summary>
public enum TransactionType
{
    /// <summary>
    /// A payment transaction (purchase, bill payment, etc.)
    /// </summary>
    Payment,

    /// <summary>
    /// A transfer between accounts
    /// </summary>
    Transfer,

    /// <summary>
    /// An authorization (hold on funds)
    /// </summary>
    Authorization,

    /// <summary>
    /// A refund transaction
    /// </summary>
    Refund
}
