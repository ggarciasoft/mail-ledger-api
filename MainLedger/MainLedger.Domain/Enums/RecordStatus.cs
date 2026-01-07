namespace MainLedger.Domain.Enums;

/// <summary>
/// Status of a financial record in the confirmation workflow.
/// </summary>
public enum RecordStatus
{
    /// <summary>
    /// Extracted but awaiting user confirmation
    /// </summary>
    Pending,

    /// <summary>
    /// Confirmed by user and immutable
    /// </summary>
    Confirmed,

    /// <summary>
    /// Rejected by user
    /// </summary>
    Rejected
}
