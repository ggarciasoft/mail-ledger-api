namespace MainLedger.Domain.Enums;

/// <summary>
/// Direction of money flow from the user's perspective.
/// </summary>
public enum TransactionDirection
{
    /// <summary>
    /// Money coming in (credit, deposit, refund)
    /// </summary>
    In,

    /// <summary>
    /// Money going out (debit, payment, transfer out)
    /// </summary>
    Out
}
