using MainLedger.Domain.Common;
using MainLedger.Domain.Enums;

namespace MainLedger.Domain.Entities;

/// <summary>
/// Represents a user's workflow automation configuration.
/// Defines how and when email processing jobs are executed.
/// </summary>
public sealed class WorkflowConfiguration : Entity
{
    public Guid UserId { get; private set; }
    public WorkflowMode Mode { get; private set; }

    // Separate mode schedules (cron expressions)
    public string? EmailSyncSchedule { get; private set; }
    public string? ClassificationSchedule { get; private set; }
    public string? ExtractionSchedule { get; private set; }

    // Sequential mode schedule
    public string? PipelineSchedule { get; private set; }

    // Job configuration
    public int EmailSyncBatchSize { get; private set; }
    public int ClassificationBatchSize { get; private set; }
    public int ExtractionBatchSize { get; private set; }

    // Timezone for schedule execution (IANA timezone ID, e.g., "America/New_York")
    public string TimeZoneId { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private WorkflowConfiguration(
        Guid id,
        Guid userId,
        WorkflowMode mode,
        int emailSyncBatchSize,
        int classificationBatchSize,
        int extractionBatchSize
    )
        : base(id)
    {
        UserId = userId;
        Mode = mode;
        EmailSyncBatchSize = emailSyncBatchSize;
        ClassificationBatchSize = classificationBatchSize;
        ExtractionBatchSize = extractionBatchSize;
        TimeZoneId = "UTC"; // Default to UTC
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a new workflow configuration with default settings (Manual mode).
    /// </summary>
    public static WorkflowConfiguration Create(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        return new WorkflowConfiguration(
            Guid.NewGuid(),
            userId,
            WorkflowMode.Manual,
            emailSyncBatchSize: 50,
            classificationBatchSize: 20,
            extractionBatchSize: 20
        );
    }

    /// <summary>
    /// Sets the workflow mode.
    /// </summary>
    public void SetMode(WorkflowMode mode)
    {
        Mode = mode;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets schedules for separate mode (each job runs independently).
    /// </summary>
    public void SetSeparateSchedules(
        string? emailSyncSchedule,
        string? classificationSchedule,
        string? extractionSchedule
    )
    {
        EmailSyncSchedule = emailSyncSchedule;
        ClassificationSchedule = classificationSchedule;
        ExtractionSchedule = extractionSchedule;
        PipelineSchedule = null; // Clear sequential schedule
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets schedule for sequential mode (jobs run in pipeline).
    /// </summary>
    public void SetPipelineSchedule(string? pipelineSchedule)
    {
        PipelineSchedule = pipelineSchedule;
        EmailSyncSchedule = null; // Clear separate schedules
        ClassificationSchedule = null;
        ExtractionSchedule = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets batch sizes for all job types.
    /// </summary>
    public void SetBatchSizes(
        int emailSyncBatchSize,
        int classificationBatchSize,
        int extractionBatchSize
    )
    {
        if (emailSyncBatchSize < 1 || emailSyncBatchSize > 100)
            throw new ArgumentException(
                "Email sync batch size must be between 1 and 100.",
                nameof(emailSyncBatchSize)
            );

        if (classificationBatchSize < 1 || classificationBatchSize > 100)
            throw new ArgumentException(
                "Classification batch size must be between 1 and 100.",
                nameof(classificationBatchSize)
            );

        if (extractionBatchSize < 1 || extractionBatchSize > 100)
            throw new ArgumentException(
                "Extraction batch size must be between 1 and 100.",
                nameof(extractionBatchSize)
            );

        EmailSyncBatchSize = emailSyncBatchSize;
        ClassificationBatchSize = classificationBatchSize;
        ExtractionBatchSize = extractionBatchSize;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the timezone for schedule execution.
    /// </summary>
    public void SetTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            throw new ArgumentException("Timezone ID cannot be empty.", nameof(timeZoneId));

        // Validate timezone ID
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            throw new ArgumentException($"Invalid timezone ID: {timeZoneId}", nameof(timeZoneId));
        }

        TimeZoneId = timeZoneId;
        UpdatedAt = DateTime.UtcNow;
    }

    // For EF Core
    private WorkflowConfiguration()
        : base() { }
}
