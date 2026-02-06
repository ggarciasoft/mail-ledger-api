namespace MainLedger.Domain.Settings;

/// <summary>
/// Configuration settings for Stripe payment integration.
/// </summary>
public class StripeSettings
{
    /// <summary>
    /// Stripe secret API key (starts with sk_).
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Stripe publishable key for frontend (starts with pk_).
    /// </summary>
    public string PublishableKey { get; set; } = string.Empty;

    /// <summary>
    /// Webhook signing secret for validating webhook events (starts with whsec_).
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>
    /// Mapping of plan names to Stripe Price IDs.
    /// Example: { "Basic": "price_1234...", "Pro": "price_5678..." }
    /// </summary>
    public Dictionary<string, string> PlanPriceIds { get; set; } = new();

    /// <summary>
    /// Success URL for Stripe Checkout redirect.
    /// </summary>
    public string CheckoutSuccessUrl { get; set; } = string.Empty;

    /// <summary>
    /// Cancel URL for Stripe Checkout redirect.
    /// </summary>
    public string CheckoutCancelUrl { get; set; } = string.Empty;
}
