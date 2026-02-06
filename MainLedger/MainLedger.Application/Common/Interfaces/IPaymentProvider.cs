namespace MainLedger.Application.Common.Interfaces;

/// <summary>
/// Provider-agnostic interface for payment processing.
/// Allows swapping payment providers (Stripe, PayPal, etc.) without changing business logic.
/// </summary>
public interface IPaymentProvider
{
    /// <summary>
    /// Creates a checkout session for subscription payment.
    /// </summary>
    /// <param name="userEmail">User's email address</param>
    /// <param name="planPriceId">Payment provider's price ID for the plan</param>
    /// <param name="successUrl">URL to redirect on successful payment</param>
    /// <param name="cancelUrl">URL to redirect if payment is cancelled</param>
    /// <param name="metadata">Additional metadata to attach to the session</param>
    /// <returns>Checkout session URL for redirect</returns>
    Task<string> CreateCheckoutSessionAsync(
        string userEmail,
        string planPriceId,
        string successUrl,
        string cancelUrl,
        Dictionary<string, string> metadata,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Validates webhook signature to ensure request is from the payment provider.
    /// </summary>
    /// <param name="payload">Raw webhook payload</param>
    /// <param name="signature">Signature header from webhook request</param>
    /// <param name="secret">Webhook signing secret</param>
    /// <returns>True if signature is valid</returns>
    Task<bool> ValidateWebhookSignatureAsync(
        string payload,
        string signature,
        string secret
    );

    /// <summary>
    /// Processes a webhook event from the payment provider.
    /// </summary>
    /// <param name="eventJson">Raw webhook event JSON</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task HandleWebhookEventAsync(string eventJson, CancellationToken cancellationToken = default);
}
