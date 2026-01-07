namespace MainLedger.Domain.Enums;

/// <summary>
/// Categories of financial emails.
/// </summary>
public enum EmailCategory
{
    /// <summary>
    /// Payment made or received.
    /// </summary>
    Payment,

    /// <summary>
    /// Money transfer between accounts.
    /// </summary>
    Transfer,

    /// <summary>
    /// Receipt or confirmation of purchase.
    /// </summary>
    Receipt,

    /// <summary>
    /// Invoice or bill.
    /// </summary>
    Invoice,

    /// <summary>
    /// Account statement.
    /// </summary>
    Statement,

    /// <summary>
    /// Authorization or pre-authorization.
    /// </summary>
    Authorization,

    /// <summary>
    /// Refund or reversal.
    /// </summary>
    Refund,

    /// <summary>
    /// Other financial activity.
    /// </summary>
    Other
}
