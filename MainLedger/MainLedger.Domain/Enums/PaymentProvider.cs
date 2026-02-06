namespace MainLedger.Domain.Enums;

/// <summary>
/// Represents the payment provider used for a subscription.
/// </summary>
public enum PaymentProvider
{
    /// <summary>
    /// No payment provider (free plan).
    /// </summary>
    None = 0,

    /// <summary>
    /// Stripe payment provider.
    /// </summary>
    Stripe = 1,

    /// <summary>
    /// Manual/admin override.
    /// </summary>
    Manual = 99
}
