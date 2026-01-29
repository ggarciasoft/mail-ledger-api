using MainLedger.Domain.Enums;

namespace MainLedger.Domain.Entities;

/// <summary>
/// Represents a webhook delivery attempt with tracking information
/// </summary>
public class WebhookDelivery
{
    /// <summary>
    /// Unique identifier for this delivery attempt
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// The webhook endpoint this delivery is for
    /// </summary>
    public Guid WebhookEndpointId { get; set; }
    
    /// <summary>
    /// Type of event that triggered this webhook
    /// </summary>
    public WebhookEventType EventType { get; set; }
    
    /// <summary>
    /// JSON payload that was/will be sent
    /// </summary>
    public string Payload { get; set; } = string.Empty;
    
    /// <summary>
    /// Current status of this delivery
    /// </summary>
    public WebhookDeliveryStatus Status { get; set; } = WebhookDeliveryStatus.Pending;
    
    /// <summary>
    /// Number of delivery attempts made
    /// </summary>
    public int AttemptCount { get; set; } = 0;
    
    /// <summary>
    /// When the last delivery attempt was made
    /// </summary>
    public DateTime? LastAttemptAt { get; set; }
    
    /// <summary>
    /// HTTP status code from the last delivery attempt
    /// </summary>
    public int? ResponseStatusCode { get; set; }
    
    /// <summary>
    /// Response body from the last delivery attempt (truncated to 1000 chars)
    /// </summary>
    public string? ResponseBody { get; set; }
    
    /// <summary>
    /// Error message if delivery failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// When this delivery record was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Navigation property to the webhook endpoint
    /// </summary>
    public WebhookEndpoint WebhookEndpoint { get; set; } = null!;
}
