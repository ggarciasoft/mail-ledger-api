using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;

namespace MainLedger.Application.Common.Interfaces;

/// <summary>
/// Service for managing processing job lifecycle.
/// </summary>
public interface IJobManagementService
{
    /// <summary>
    /// Creates a new processing job with race condition protection.
    /// Uses double-check pattern to prevent duplicate jobs.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="jobType">Type of job to create</param>
    /// <param name="metadata">Job metadata (serialized JSON)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created job or null if a duplicate was detected</returns>
    Task<ProcessingJob?> CreateJobAsync(
        Guid userId,
        JobType jobType,
        string metadata,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Updates an existing job in the database.
    /// </summary>
    /// <param name="job">Job to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateJobAsync(ProcessingJob job, CancellationToken cancellationToken = default);
}
