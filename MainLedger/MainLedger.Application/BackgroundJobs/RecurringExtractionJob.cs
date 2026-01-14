using Hangfire;
using MainLedger.Application.Common.Interfaces;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.BackgroundJobs;

/// <summary>
/// Recurring job for extraction in separate mode.
/// </summary>
public class RecurringExtractionJob
{
    private readonly IWorkflowConfigurationRepository _configRepository;
    private readonly IJobManagementService _jobManagementService;
    private readonly ILogger<RecurringExtractionJob> _logger;

    public RecurringExtractionJob(
        IWorkflowConfigurationRepository configRepository,
        IJobManagementService jobManagementService,
        ILogger<RecurringExtractionJob> logger
    )
    {
        _configRepository = configRepository;
        _jobManagementService = jobManagementService;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid userId, int batchSize)
    {
        _logger.LogInformation("Recurring extraction triggered for user {UserId}", userId);

        // Verify user still has separate mode enabled
        var config = await _configRepository.GetByUserIdAsync(userId);
        if (config?.Mode != WorkflowMode.Separate)
        {
            _logger.LogWarning(
                "User {UserId} no longer has separate mode enabled, skipping extraction",
                userId
            );
            return;
        }

        // Create and enqueue job
        var job = await _jobManagementService.CreateJobAsync(
            userId,
            JobType.Extraction,
            System.Text.Json.JsonSerializer.Serialize(new { batchSize })
        );

        if (job != null)
        {
            var hangfireJobId = BackgroundJob.Enqueue<ExtractionBackgroundJob>(x =>
                x.ExecuteAsync(job.Id, userId, batchSize, CancellationToken.None)
            );

            job.SetHangfireJobId(hangfireJobId);
            await _jobManagementService.UpdateJobAsync(job);

            _logger.LogInformation(
                "Enqueued extraction job {JobId} for user {UserId}",
                job.Id,
                userId
            );
        }
        else
        {
            _logger.LogInformation(
                "Extraction job already running for user {UserId}, skipping",
                userId
            );
        }
    }
}
