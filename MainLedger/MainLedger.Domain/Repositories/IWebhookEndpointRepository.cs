using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;

namespace MainLedger.Domain.Repositories;

/// <summary>
/// Repository interface for webhook endpoint operations
/// </summary>
public interface IWebhookEndpointRepository
{
    /// <summary>
    /// Get a webhook endpoint by ID
    /// </summary>
    Task<WebhookEndpoint?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all webhook endpoints for a user
    /// </summary>
    Task<List<WebhookEndpoint>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get active webhook endpoints for a user that are subscribed to a specific event type
    /// </summary>
    Task<List<WebhookEndpoint>> GetActiveByUserIdAndEventTypeAsync(
        Guid userId, 
        WebhookEventType eventType, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Add a new webhook endpoint
    /// </summary>
    Task AddAsync(WebhookEndpoint webhookEndpoint, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update an existing webhook endpoint
    /// </summary>
    Task UpdateAsync(WebhookEndpoint webhookEndpoint, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Delete a webhook endpoint
    /// </summary>
    Task DeleteAsync(WebhookEndpoint webhookEndpoint, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Count webhook endpoints for a user
    /// </summary>
    Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
