using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;

namespace MainLedger.Domain.Repositories;

/// <summary>
/// Repository interface for webhook delivery operations
/// </summary>
public interface IWebhookDeliveryRepository
{
    /// <summary>
    /// Get a webhook delivery by ID
    /// </summary>
    Task<WebhookDelivery?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get webhook deliveries for a specific endpoint with optional filtering
    /// </summary>
    Task<(List<WebhookDelivery> Deliveries, int TotalCount)> GetByEndpointIdAsync(
        Guid endpointId,
        WebhookDeliveryStatus? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Add a new webhook delivery record
    /// </summary>
    Task AddAsync(WebhookDelivery webhookDelivery, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update an existing webhook delivery record
    /// </summary>
    Task UpdateAsync(WebhookDelivery webhookDelivery, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Delete old webhook deliveries (for cleanup)
    /// </summary>
    Task DeleteOlderThanAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get webhook deliveries stuck in Pending status older than the cutoff time
    /// </summary>
    Task<List<WebhookDelivery>> GetStuckPendingDeliveriesAsync(DateTime cutoffTime, CancellationToken cancellationToken = default);
}
