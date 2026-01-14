using Hangfire;
using MainLedger.Application.Common.Interfaces;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.BackgroundJobs;

/// <summary>
/// Recurring job for sequential pipeline mode.
/// Runs email sync, classification, and extraction in sequence.
/// </summary>
public class SequentialPipelineJob
{
    private readonly IWorkflowConfigurationRepository _configRepository;
    private readonly IJobManagementService _jobManagementService;
    private readonly IProcessingJobRepository _jobRepository;
    private readonly ILogger<SequentialPipelineJob> _logger;

    public SequentialPipelineJob(
        IWorkflowConfigurationRepository configRepository,
        IJobManagementService _jobManagementService,
        IProcessingJobRepository jobRepository,
        ILogger<SequentialPipelineJob> logger
    )
    {
        _configRepository = configRepository;
        this._jobManagementService = _jobManagementService;
        _jobRepository = jobRepository;
        _logger = logger;
    }

    public async Task ExecuteAsync(
        Guid userId,
        int syncBatchSize,
        int classifyBatchSize,
        int extractBatchSize
    )
    {
        _logger.LogInformation("Sequential pipeline triggered for user {UserId}", userId);

        // Verify user still has sequential mode enabled
        var config = await _configRepository.GetByUserIdAsync(userId);
        if (config?.Mode != WorkflowMode.Sequential)
        {
            _logger.LogWarning(
                "User {UserId} no longer has sequential mode enabled, skipping pipeline",
                userId
            );
            return;
        }

        // Step 1: Email Sync
        var syncJob = await _jobManagementService.CreateJobAsync(
            userId,
            JobType.EmailSync,
            System.Text.Json.JsonSerializer.Serialize(new { maxEmails = syncBatchSize })
        );

        if (syncJob != null)
        {
            var syncHangfireId = BackgroundJob.Enqueue<EmailSyncBackgroundJob>(x =>
                x.ExecuteAsync(syncJob.Id, userId, syncBatchSize, CancellationToken.None)
            );

            syncJob.SetHangfireJobId(syncHangfireId);
            await _jobManagementService.UpdateJobAsync(syncJob);

            // Step 2: Classification (continues after sync completes)
            var classifyHangfireId = BackgroundJob.ContinueJobWith<SequentialPipelineJob>(
                syncHangfireId,
                x => x.TriggerClassificationAsync(userId, classifyBatchSize, extractBatchSize)
            );

            _logger.LogInformation(
                "Enqueued sequential pipeline for user {UserId}: Sync={SyncJobId}, Classify={ClassifyJobId}",
                userId,
                syncJob.Id,
                classifyHangfireId
            );
        }
        else
        {
            _logger.LogWarning(
                "Email sync job already running for user {UserId}, skipping pipeline",
                userId
            );
        }
    }

    /// <summary>
    /// Triggers classification step of the pipeline.
    /// </summary>
    public async Task TriggerClassificationAsync(
        Guid userId,
        int classifyBatchSize,
        int extractBatchSize
    )
    {
        _logger.LogInformation(
            "Sequential pipeline - Classification step for user {UserId}",
            userId
        );

        var classifyJob = await _jobManagementService.CreateJobAsync(
            userId,
            JobType.Classification,
            System.Text.Json.JsonSerializer.Serialize(new { batchSize = classifyBatchSize })
        );

        if (classifyJob != null)
        {
            var classifyHangfireId = BackgroundJob.Enqueue<ClassificationBackgroundJob>(x =>
                x.ExecuteAsync(classifyJob.Id, userId, classifyBatchSize, CancellationToken.None)
            );

            classifyJob.SetHangfireJobId(classifyHangfireId);
            await _jobManagementService.UpdateJobAsync(classifyJob);

            // Step 3: Extraction (continues after classification completes)
            var extractHangfireId = BackgroundJob.ContinueJobWith<SequentialPipelineJob>(
                classifyHangfireId,
                x => x.TriggerExtractionAsync(userId, extractBatchSize)
            );

            _logger.LogInformation(
                "Enqueued classification and extraction for user {UserId}: Classify={ClassifyJobId}, Extract={ExtractJobId}",
                userId,
                classifyJob.Id,
                extractHangfireId
            );
        }
    }

    /// <summary>
    /// Triggers extraction step of the pipeline.
    /// </summary>
    public async Task TriggerExtractionAsync(Guid userId, int extractBatchSize)
    {
        _logger.LogInformation("Sequential pipeline - Extraction step for user {UserId}", userId);

        var extractJob = await _jobManagementService.CreateJobAsync(
            userId,
            JobType.Extraction,
            System.Text.Json.JsonSerializer.Serialize(new { batchSize = extractBatchSize })
        );

        if (extractJob != null)
        {
            var extractHangfireId = BackgroundJob.Enqueue<ExtractionBackgroundJob>(x =>
                x.ExecuteAsync(extractJob.Id, userId, extractBatchSize, CancellationToken.None)
            );

            extractJob.SetHangfireJobId(extractHangfireId);
            await _jobManagementService.UpdateJobAsync(extractJob);

            _logger.LogInformation(
                "Enqueued extraction job {JobId} for user {UserId}",
                extractJob.Id,
                userId
            );
        }
    }
}
