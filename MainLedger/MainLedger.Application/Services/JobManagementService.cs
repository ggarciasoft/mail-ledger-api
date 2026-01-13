using MainLedger.Application.Common.Interfaces;
using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.Services;

/// <summary>
/// Service for managing processing job lifecycle.
/// Handles job creation with race condition protection and Hangfire integration.
/// </summary>
public class JobManagementService : IJobManagementService
{
    private readonly IProcessingJobRepository _jobRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<JobManagementService> _logger;

    public JobManagementService(
        IProcessingJobRepository jobRepository,
        IUnitOfWork unitOfWork,
        ILogger<JobManagementService> logger
    )
    {
        _jobRepository = jobRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new processing job with race condition protection.
    /// Uses double-check pattern to prevent duplicate jobs.
    /// </summary>
    public async Task<ProcessingJob?> CreateJobAsync(
        Guid userId,
        JobType jobType,
        string metadata,
        CancellationToken cancellationToken = default
    )
    {
        // First check: Verify no active job exists
        var hasActiveJob = await _jobRepository.HasActiveJobOfTypeAsync(
            userId,
            jobType,
            cancellationToken
        );

        if (hasActiveJob)
        {
            _logger.LogWarning("{JobType} job already running for user {UserId}", jobType, userId);
            return null;
        }

        // Create and save the job
        var job = ProcessingJob.Create(userId, jobType, string.Empty, metadata);
        await _jobRepository.AddAsync(job, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Double-check after save to catch race condition
        // If two requests passed the first check simultaneously, one will be caught here
        var activeJobsAfterSave = await _jobRepository.GetActiveJobsForUserAsync(
            userId,
            jobType,
            cancellationToken
        );

        if (activeJobsAfterSave.Count > 1)
        {
            _logger.LogWarning(
                "Race condition detected: Multiple {JobType} jobs created for user {UserId}. Rejecting duplicate.",
                jobType,
                userId
            );

            // Check if our job is the duplicate (newer one)
            var oldestJob = activeJobsAfterSave.OrderBy(j => j.CreatedAt).First();

            if (oldestJob.Id != job.Id)
            {
                // Our job is the duplicate, return null
                return null;
            }
        }

        _logger.LogInformation(
            "Created {JobType} job {JobId} for user {UserId}",
            jobType,
            job.Id,
            userId
        );

        return job;
    }
}
