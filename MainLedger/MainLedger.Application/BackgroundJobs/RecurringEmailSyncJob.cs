using Hangfire;
using MainLedger.Application.Common.Interfaces;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.BackgroundJobs;

/// <summary>
/// Recurring job for email sync in separate mode.
/// </summary>
public class RecurringEmailSyncJob
{
    private readonly IWorkflowConfigurationRepository _configRepository;
    private readonly IJobManagementService _jobManagementService;
    private readonly ILogger<RecurringEmailSyncJob> _logger;

    public RecurringEmailSyncJob(
        IWorkflowConfigurationRepository configRepository,
        IJobManagementService jobManagementService,
        ILogger<RecurringEmailSyncJob> logger
    )
    {
        _configRepository = configRepository;
        _jobManagementService = jobManagementService;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid userId, int batchSize)
    {
        _logger.LogInformation("Recurring email sync triggered for user {UserId}", userId);

        // Verify user still has separate mode enabled
        var config = await _configRepository.GetByUserIdAsync(userId);
        if (config?.Mode != WorkflowMode.Separate)
        {
            _logger.LogWarning(
                "User {UserId} no longer has separate mode enabled, skipping email sync",
                userId
            );
            return;
        }

        // Create and enqueue job
        var job = await _jobManagementService.CreateJobAsync(
            userId,
            JobType.EmailSync,
            System.Text.Json.JsonSerializer.Serialize(new { maxEmails = batchSize })
        );

        if (job != null)
        {
            var hangfireJobId = BackgroundJob.Enqueue<EmailSyncBackgroundJob>(x =>
                x.ExecuteAsync(job.Id, userId, batchSize, CancellationToken.None)
            );

            job.SetHangfireJobId(hangfireJobId);
            await _jobManagementService.UpdateJobAsync(job);

            _logger.LogInformation(
                "Enqueued email sync job {JobId} for user {UserId}",
                job.Id,
                userId
            );
        }
        else
        {
            _logger.LogInformation(
                "Email sync job already running for user {UserId}, skipping",
                userId
            );
        }
    }
}
