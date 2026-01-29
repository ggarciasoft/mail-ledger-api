namespace MainLedger.Domain.Enums;

/// <summary>
/// Status of a webhook delivery attempt
/// </summary>
public enum WebhookDeliveryStatus
{
    /// <summary>
    /// Delivery is pending (not yet attempted)
    /// </summary>
    Pending = 1,
    
    /// <summary>
    /// Delivery was successful (HTTP 2xx response)
    /// </summary>
    Success = 2,
    
    /// <summary>
    /// Delivery failed after all retry attempts
    /// </summary>
    Failed = 3
}
