namespace MainLedger.Domain.Enums;

/// <summary>
/// Types of events that can trigger webhook notifications
/// </summary>
public enum WebhookEventType
{
    /// <summary>
    /// Triggered when a single extraction candidate is confirmed
    /// </summary>
    CandidateConfirmed = 1,
    
    /// <summary>
    /// Triggered when multiple extraction candidates are confirmed in bulk
    /// </summary>
    CandidateBulkConfirmed = 2
}
