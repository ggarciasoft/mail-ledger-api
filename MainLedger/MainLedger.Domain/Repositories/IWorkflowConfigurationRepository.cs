using MainLedger.Domain.Entities;

namespace MainLedger.Domain.Repositories;

/// <summary>
/// Repository for workflow configuration operations.
/// </summary>
public interface IWorkflowConfigurationRepository
{
    /// <summary>
    /// Gets the workflow configuration for a specific user.
    /// </summary>
    Task<WorkflowConfiguration?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Adds a new workflow configuration.
    /// </summary>
    Task AddAsync(
        WorkflowConfiguration configuration,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Updates an existing workflow configuration.
    /// </summary>
    void Update(WorkflowConfiguration configuration);
}
