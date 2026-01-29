using MainLedger.Domain.Enums;

namespace MainLedger.Application.Common.Interfaces;

/// <summary>
/// Service for managing webhook delivery and signature validation
/// </summary>
public interface IWebhookService
{
    /// <summary>
    /// Trigger webhooks for a specific user and event type
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="eventType">Type of event that occurred</param>
    /// <param name="payload">Payload to send (will be serialized to JSON)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task TriggerWebhooksAsync(
        Guid userId, 
        WebhookEventType eventType, 
        object payload, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generate HMAC-SHA256 signature for a payload
    /// </summary>
    /// <param name="payload">JSON payload</param>
    /// <param name="secretKey">Secret key</param>
    /// <returns>Base64-encoded signature</returns>
    string GenerateSignature(string payload, string secretKey);
    
    /// <summary>
    /// Validate HMAC-SHA256 signature
    /// </summary>
    /// <param name="payload">JSON payload</param>
    /// <param name="signature">Signature to validate</param>
    /// <param name="secretKey">Secret key</param>
    /// <returns>True if signature is valid</returns>
    bool ValidateSignature(string payload, string signature, string secretKey);
}
