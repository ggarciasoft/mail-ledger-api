using MainLedger.Domain.Enums;

namespace MainLedger.Contracts.Workflow;

/// <summary>
/// Response DTO for workflow configuration.
/// </summary>
public record WorkflowConfigurationDto(
    WorkflowMode Mode,
    string? EmailSyncSchedule,
    string? ClassificationSchedule,
    string? ExtractionSchedule,
    string? PipelineSchedule,
    int EmailSyncBatchSize,
    int ClassificationBatchSize,
    int ExtractionBatchSize,
    string TimeZoneId
);

/// <summary>
/// Request DTO for updating workflow configuration.
/// </summary>
public record UpdateWorkflowConfigDto(
    WorkflowMode Mode,
    string? EmailSyncSchedule,
    string? ClassificationSchedule,
    string? ExtractionSchedule,
    string? PipelineSchedule,
    int EmailSyncBatchSize,
    int ClassificationBatchSize,
    int ExtractionBatchSize,
    string TimeZoneId
);
