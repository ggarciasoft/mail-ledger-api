using Hangfire;
using MainLedger.Application.BackgroundJobs;
using MainLedger.Application.Common.Interfaces;
using MainLedger.Contracts.Workflow;
using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.Services;

/// <summary>
/// Service for managing workflow automation configuration and Hangfire recurring jobs.
/// </summary>
public class WorkflowService : IWorkflowService
{
    private readonly IWorkflowConfigurationRepository _repository;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WorkflowService> _logger;

    public WorkflowService(
        IWorkflowConfigurationRepository repository,
        ISubscriptionService subscriptionService,
        IUnitOfWork unitOfWork,
        ILogger<WorkflowService> logger
    )
    {
        _repository = repository;
        _subscriptionService = subscriptionService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<WorkflowConfigurationDto> GetConfigurationAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var config = await _repository.GetByUserIdAsync(userId, cancellationToken);

        // Create default configuration if none exists
        if (config == null)
        {
            config = WorkflowConfiguration.Create(userId);
            await _repository.AddAsync(config, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Created default workflow configuration for user {UserId}",
                userId
            );
        }

        return MapToDto(config);
    }

    public async Task UpdateConfigurationAsync(
        Guid userId,
        UpdateWorkflowConfigDto dto,
        CancellationToken cancellationToken = default
    )
    {
        var config = await _repository.GetByUserIdAsync(userId, cancellationToken);

        if (config == null)
        {
            config = WorkflowConfiguration.Create(userId);
            await _repository.AddAsync(config, cancellationToken);
        }
        else
        {
            _repository.Update(config);
        }

        // Check subscription permissions for workflow automation
        if (dto.Mode != WorkflowMode.Manual)
        {
            var canUseWorkflow = await _subscriptionService.CanUseWorkflowAutomationAsync(
                userId,
                cancellationToken
            );
            if (!canUseWorkflow)
            {
                throw new InvalidOperationException(
                    "Workflow automation is not available on your subscription plan. Please upgrade to use automated workflows."
                );
            }
        }

        // Update configuration
        config.SetMode(dto.Mode);
        config.SetBatchSizes(
            dto.EmailSyncBatchSize,
            dto.ClassificationBatchSize,
            dto.ExtractionBatchSize
        );
        config.SetTimeZone(dto.TimeZoneId);

        // Remove all existing recurring jobs for this user
        RemoveUserRecurringJobs(userId);

        // Set up new recurring jobs based on mode
        if (dto.Mode == WorkflowMode.Separate)
        {
            config.SetSeparateSchedules(
                dto.EmailSyncSchedule,
                dto.ClassificationSchedule,
                dto.ExtractionSchedule
            );
            SetupSeparateJobs(userId, config);
        }
        else if (dto.Mode == WorkflowMode.Sequential)
        {
            config.SetPipelineSchedule(dto.PipelineSchedule);
            SetupSequentialJob(userId, config);
        }
        // Manual mode: no recurring jobs

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated workflow configuration for user {UserId} to mode {Mode} with timezone {TimeZone}",
            userId,
            dto.Mode,
            dto.TimeZoneId
        );
    }

    private void SetupSeparateJobs(Guid userId, WorkflowConfiguration config)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(config.TimeZoneId);

        // Email Sync
        if (!string.IsNullOrWhiteSpace(config.EmailSyncSchedule))
        {
            RecurringJob.AddOrUpdate<RecurringEmailSyncJob>(
                $"email-sync-{userId}",
                job => job.ExecuteAsync(userId, config.EmailSyncBatchSize),
                config.EmailSyncSchedule,
                new RecurringJobOptions { TimeZone = timeZone }
            );

            _logger.LogInformation(
                "Scheduled email sync for user {UserId} with cron: {Cron}",
                userId,
                config.EmailSyncSchedule
            );
        }

        // Classification
        if (!string.IsNullOrWhiteSpace(config.ClassificationSchedule))
        {
            RecurringJob.AddOrUpdate<RecurringClassificationJob>(
                $"classification-{userId}",
                job => job.ExecuteAsync(userId, config.ClassificationBatchSize),
                config.ClassificationSchedule,
                new RecurringJobOptions { TimeZone = timeZone }
            );

            _logger.LogInformation(
                "Scheduled classification for user {UserId} with cron: {Cron}",
                userId,
                config.ClassificationSchedule
            );
        }

        // Extraction
        if (!string.IsNullOrWhiteSpace(config.ExtractionSchedule))
        {
            RecurringJob.AddOrUpdate<RecurringExtractionJob>(
                $"extraction-{userId}",
                job => job.ExecuteAsync(userId, config.ExtractionBatchSize),
                config.ExtractionSchedule,
                new RecurringJobOptions { TimeZone = timeZone }
            );

            _logger.LogInformation(
                "Scheduled extraction for user {UserId} with cron: {Cron}",
                userId,
                config.ExtractionSchedule
            );
        }
    }

    private void SetupSequentialJob(Guid userId, WorkflowConfiguration config)
    {
        if (!string.IsNullOrWhiteSpace(config.PipelineSchedule))
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(config.TimeZoneId);

            RecurringJob.AddOrUpdate<SequentialPipelineJob>(
                $"pipeline-{userId}",
                job =>
                    job.ExecuteAsync(
                        userId,
                        config.EmailSyncBatchSize,
                        config.ClassificationBatchSize,
                        config.ExtractionBatchSize
                    ),
                config.PipelineSchedule,
                new RecurringJobOptions { TimeZone = timeZone }
            );

            _logger.LogInformation(
                "Scheduled sequential pipeline for user {UserId} with cron: {Cron}",
                userId,
                config.PipelineSchedule
            );
        }
    }

    private void RemoveUserRecurringJobs(Guid userId)
    {
        RecurringJob.RemoveIfExists($"email-sync-{userId}");
        RecurringJob.RemoveIfExists($"classification-{userId}");
        RecurringJob.RemoveIfExists($"extraction-{userId}");
        RecurringJob.RemoveIfExists($"pipeline-{userId}");

        _logger.LogInformation("Removed all recurring jobs for user {UserId}", userId);
    }

    private static WorkflowConfigurationDto MapToDto(WorkflowConfiguration config)
    {
        return new WorkflowConfigurationDto(
            config.Mode,
            config.EmailSyncSchedule,
            config.ClassificationSchedule,
            config.ExtractionSchedule,
            config.PipelineSchedule,
            config.EmailSyncBatchSize,
            config.ClassificationBatchSize,
            config.ExtractionBatchSize,
            config.TimeZoneId
        );
    }
}
