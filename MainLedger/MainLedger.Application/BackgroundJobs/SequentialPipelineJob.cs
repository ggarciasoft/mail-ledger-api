using Hangfire;
using MainLedger.Application.Common.Interfaces;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.BackgroundJobs;

/// <summary>
/// Recurring job for sequential pipeline mode.
/// Runs email sync, classification, and extraction in sequence.
/// Uses Hangfire's job state monitoring for proper sequencing.
/// </summary>
public class SequentialPipelineJob
{
    private readonly IWorkflowConfigurationRepository _configRepository;
    private readonly IJobManagementService _jobManagementService;
    private readonly IProcessingJobRepository _jobRepository;
    private readonly ILogger<SequentialPipelineJob> _logger;

    public SequentialPipelineJob(
        IWorkflowConfigurationRepository configRepository,
        IJobManagementService jobManagementService,
        IProcessingJobRepository jobRepository,
        ILogger<SequentialPipelineJob> logger
    )
    {
        _configRepository = configRepository;
        _jobManagementService = jobManagementService;
        _jobRepository = jobRepository;
        _logger = logger;
    }

    /// <summary>
    /// Entry point: Starts the pipeline with email sync.
    /// </summary>
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

            // Schedule classification to run after sync completes
            BackgroundJob.ContinueJobWith<SequentialPipelineJob>(
                syncHangfireId,
                x =>
                    x.RunClassificationStepAsync(
                        userId,
                        syncJob.Id,
                        classifyBatchSize,
                        extractBatchSize
                    )
            );

            _logger.LogInformation(
                "Started sequential pipeline for user {UserId}: EmailSync job {JobId}",
                userId,
                syncJob.Id
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
    /// Step 2: Runs after email sync completes.
    /// Checks sync status and starts classification if successful.
    /// </summary>
    public async Task RunClassificationStepAsync(
        Guid userId,
        Guid syncJobId,
        int classifyBatchSize,
        int extractBatchSize
    )
    {
        _logger.LogInformation(
            "Sequential pipeline - Classification step for user {UserId} after sync job {SyncJobId}",
            userId,
            syncJobId
        );

        // Verify sync job completed successfully
        var syncJob = await _jobRepository.GetByIdAsync(syncJobId);
        if (syncJob == null || syncJob.Status != JobStatus.Completed)
        {
            _logger.LogWarning(
                "Email sync job {SyncJobId} did not complete successfully (Status: {Status}), stopping pipeline",
                syncJobId,
                syncJob?.Status
            );
            return;
        }

        // Start classification
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

            // Schedule extraction to run after classification completes
            BackgroundJob.ContinueJobWith<SequentialPipelineJob>(
                classifyHangfireId,
                x => x.RunExtractionStepAsync(userId, classifyJob.Id, extractBatchSize)
            );

            _logger.LogInformation(
                "Started classification for user {UserId}: job {JobId}",
                userId,
                classifyJob.Id
            );
        }
    }

    /// <summary>
    /// Step 3: Runs after classification completes.
    /// Checks classification status and starts extraction if successful.
    /// </summary>
    public async Task RunExtractionStepAsync(Guid userId, Guid classifyJobId, int extractBatchSize)
    {
        _logger.LogInformation(
            "Sequential pipeline - Extraction step for user {UserId} after classification job {ClassifyJobId}",
            userId,
            classifyJobId
        );

        // Verify classification job completed successfully
        var classifyJob = await _jobRepository.GetByIdAsync(classifyJobId);
        if (classifyJob == null || classifyJob.Status != JobStatus.Completed)
        {
            _logger.LogWarning(
                "Classification job {ClassifyJobId} did not complete successfully (Status: {Status}), stopping pipeline",
                classifyJobId,
                classifyJob?.Status
            );
            return;
        }

        // Start extraction
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
                "Started extraction for user {UserId}: job {JobId}. Pipeline complete.",
                userId,
                extractJob.Id
            );
        }
    }
}
