using MainLedger.Domain.Enums;

namespace MainLedger.Domain.Entities;

/// <summary>
/// Represents a webhook endpoint configuration for a user
/// </summary>
public class WebhookEndpoint
{
    /// <summary>
    /// Unique identifier for the webhook endpoint
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// User who owns this webhook endpoint
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Target URL where webhook POST requests will be sent
    /// </summary>
    public string Url { get; set; } = string.Empty;
    
    /// <summary>
    /// Secret key used for HMAC-SHA256 signature generation (encrypted in database)
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;
    
    /// <summary>
    /// List of event types this endpoint is subscribed to
    /// </summary>
    public List<WebhookEventType> Events { get; set; } = new();
    
    /// <summary>
    /// Whether this webhook endpoint is active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// When this webhook endpoint was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// When this webhook was last successfully triggered
    /// </summary>
    public DateTime? LastTriggeredAt { get; set; }
    
    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public User User { get; set; } = null!;
    
    /// <summary>
    /// Navigation property to webhook deliveries
    /// </summary>
    public ICollection<WebhookDelivery> Deliveries { get; set; } = new List<WebhookDelivery>();
}
