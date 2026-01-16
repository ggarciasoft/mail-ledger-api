using MainLedger.Domain.ValueObjects;

namespace MainLedger.Application.Common.Interfaces;

/// <summary>
/// Service for managing subscription limits and permissions.
/// </summary>
public interface ISubscriptionService
{
    /// <summary>
    /// Gets the subscription limits for a user.
    /// </summary>
    Task<SubscriptionLimits> GetUserLimitsAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Checks if the user can process more emails this month.
    /// </summary>
    Task<bool> CanProcessEmailAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments the email count for the user's current billing period.
    /// </summary>
    Task IncrementEmailCountAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the user can create another API key.
    /// </summary>
    Task<bool> CanCreateApiKeyAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the user can connect another Gmail account.
    /// </summary>
    Task<bool> CanConnectGmailAccountAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Checks if the user can export data.
    /// </summary>
    Task<bool> CanExportAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the user can use workflow automation.
    /// </summary>
    Task<bool> CanUseWorkflowAutomationAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Checks if the user can use webhooks.
    /// </summary>
    Task<bool> CanUseWebhooksAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the user can use bulk operations.
    /// </summary>
    Task<bool> CanUseBulkOperationsAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets the current usage statistics for a user.
    /// </summary>
    Task<(int emailsProcessed, int emailLimit)> GetEmailUsageAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );
}
