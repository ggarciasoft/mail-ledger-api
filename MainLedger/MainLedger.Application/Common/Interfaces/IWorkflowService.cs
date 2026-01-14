using MainLedger.Contracts.Workflow;
using MainLedger.Domain.Entities;

namespace MainLedger.Application.Common.Interfaces;

/// <summary>
/// Service for managing workflow automation configuration.
/// </summary>
public interface IWorkflowService
{
    /// <summary>
    /// Gets the workflow configuration for a user.
    /// Creates default configuration if none exists.
    /// </summary>
    Task<WorkflowConfigurationDto> GetConfigurationAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Updates the workflow configuration and manages Hangfire recurring jobs.
    /// </summary>
    Task UpdateConfigurationAsync(
        Guid userId,
        UpdateWorkflowConfigDto dto,
        CancellationToken cancellationToken = default
    );
}
