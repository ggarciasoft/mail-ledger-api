using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MainLedger.Infrastructure.Persistence.Repositories;

public class ProcessingJobRepository : IProcessingJobRepository
{
    private readonly MailLedgerDbContext _context;

    public ProcessingJobRepository(MailLedgerDbContext context)
    {
        _context = context;
    }

    public async Task<ProcessingJob?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        return await _context.ProcessingJobs.FirstOrDefaultAsync(
            j => j.Id == id,
            cancellationToken
        );
    }

    public async Task<ProcessingJob?> GetByHangfireJobIdAsync(
        string hangfireJobId,
        CancellationToken cancellationToken = default
    )
    {
        return await _context.ProcessingJobs.FirstOrDefaultAsync(
            j => j.HangfireJobId == hangfireJobId,
            cancellationToken
        );
    }

    public async Task<List<ProcessingJob>> GetActiveJobsForUserAsync(
        Guid userId,
        JobType? jobType = null,
        CancellationToken cancellationToken = default
    )
    {
        var query = _context.ProcessingJobs.Where(j =>
            j.UserId == userId && (j.Status == JobStatus.Pending || j.Status == JobStatus.Running)
        );

        if (jobType.HasValue)
        {
            query = query.Where(j => j.JobType == jobType.Value);
        }

        return await query.OrderByDescending(j => j.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<List<ProcessingJob>> GetRecentJobsAsync(
        Guid userId,
        int limit = 10,
        CancellationToken cancellationToken = default
    )
    {
        return await _context
            .ProcessingJobs.Where(j => j.UserId == userId)
            .OrderByDescending(j => j.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasActiveJobOfTypeAsync(
        Guid userId,
        JobType jobType,
        CancellationToken cancellationToken = default
    )
    {
        return await _context.ProcessingJobs.AnyAsync(
            j =>
                j.UserId == userId
                && j.JobType == jobType
                && (j.Status == JobStatus.Pending || j.Status == JobStatus.Running),
            cancellationToken
        );
    }

    public async Task AddAsync(ProcessingJob job, CancellationToken cancellationToken = default)
    {
        await _context.ProcessingJobs.AddAsync(job, cancellationToken);
    }

    public void Update(ProcessingJob job)
    {
        _context.ProcessingJobs.Update(job);
    }
}
