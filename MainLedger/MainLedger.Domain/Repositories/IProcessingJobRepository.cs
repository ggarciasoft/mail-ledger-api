using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;

namespace MainLedger.Domain.Repositories;

/// <summary>
/// Repository for managing processing jobs.
/// </summary>
public interface IProcessingJobRepository
{
    Task<ProcessingJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProcessingJob?> GetByHangfireJobIdAsync(
        string hangfireJobId,
        CancellationToken cancellationToken = default
    );
    Task<List<ProcessingJob>> GetActiveJobsForUserAsync(
        Guid userId,
        JobType? jobType = null,
        CancellationToken cancellationToken = default
    );
    Task<List<ProcessingJob>> GetRecentJobsAsync(
        Guid userId,
        int limit = 10,
        CancellationToken cancellationToken = default
    );
    Task AddAsync(ProcessingJob job, CancellationToken cancellationToken = default);
    void Update(ProcessingJob job);
}
