namespace MainLedger.Domain.Enums;

/// <summary>
/// Represents the status of a user's subscription.
/// </summary>
public enum SubscriptionStatus
{
    /// <summary>
    /// Subscription is active and in good standing.
    /// </summary>
    Active,

    /// <summary>
    /// Subscription has been cancelled but may still be active until end date.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Subscription has expired and is no longer active.
    /// </summary>
    Expired,

    /// <summary>
    /// Payment is past due.
    /// </summary>
    PastDue,

    /// <summary>
    /// Subscription is in trial period.
    /// </summary>
    Trialing,
}
