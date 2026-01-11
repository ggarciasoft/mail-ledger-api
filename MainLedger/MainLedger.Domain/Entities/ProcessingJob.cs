using MainLedger.Domain.Common;
using MainLedger.Domain.Enums;

namespace MainLedger.Domain.Entities;

/// <summary>
/// Represents a background processing job for email sync, classification, or extraction.
/// Tracks job progress and status for user visibility.
/// </summary>
public sealed class ProcessingJob : Entity
{
    public Guid UserId { get; private set; }
    public JobType JobType { get; private set; }
    public string HangfireJobId { get; private set; }
    public JobStatus Status { get; private set; }
    public int Progress { get; private set; }
    public int TotalItems { get; private set; }
    public int ProcessedItems { get; private set; }
    public int SuccessCount { get; private set; }
    public int FailureCount { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string? Metadata { get; private set; } // JSON for job-specific parameters

    private ProcessingJob(
        Guid id,
        Guid userId,
        JobType jobType,
        string hangfireJobId,
        string? metadata = null
    )
        : base(id)
    {
        UserId = userId;
        JobType = jobType;
        HangfireJobId = hangfireJobId ?? throw new ArgumentNullException(nameof(hangfireJobId));
        Status = JobStatus.Pending;
        Progress = 0;
        TotalItems = 0;
        ProcessedItems = 0;
        SuccessCount = 0;
        FailureCount = 0;
        CreatedAt = DateTime.UtcNow;
        Metadata = metadata;
    }

    /// <summary>
    /// Creates a new processing job in Pending status.
    /// </summary>
    public static ProcessingJob Create(
        Guid userId,
        JobType jobType,
        string hangfireJobId,
        string? metadata = null
    )
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        return new ProcessingJob(Guid.NewGuid(), userId, jobType, hangfireJobId, metadata);
    }

    /// <summary>
    /// Marks the job as started.
    /// </summary>
    public void Start(int totalItems)
    {
        if (Status != JobStatus.Pending)
            throw new InvalidOperationException($"Cannot start job in {Status} status.");

        Status = JobStatus.Running;
        StartedAt = DateTime.UtcNow;
        TotalItems = totalItems;
        Progress = 0;
    }

    /// <summary>
    /// Updates job progress.
    /// </summary>
    public void UpdateProgress(int processedItems, int successCount, int failureCount)
    {
        if (Status != JobStatus.Running)
            throw new InvalidOperationException(
                $"Cannot update progress for job in {Status} status."
            );

        ProcessedItems = processedItems;
        SuccessCount = successCount;
        FailureCount = failureCount;

        if (TotalItems > 0)
        {
            Progress = (int)((double)processedItems / TotalItems * 100);
        }
    }

    /// <summary>
    /// Marks the job as completed successfully.
    /// </summary>
    public void Complete()
    {
        if (Status != JobStatus.Running)
            throw new InvalidOperationException($"Cannot complete job in {Status} status.");

        Status = JobStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        Progress = 100;
    }

    /// <summary>
    /// Marks the job as failed with an error message.
    /// </summary>
    public void Fail(string errorMessage)
    {
        if (Status == JobStatus.Completed)
            throw new InvalidOperationException("Cannot fail a completed job.");

        Status = JobStatus.Failed;
        CompletedAt = DateTime.UtcNow;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Marks the job as cancelled.
    /// </summary>
    public void Cancel()
    {
        if (Status == JobStatus.Completed || Status == JobStatus.Failed)
            throw new InvalidOperationException($"Cannot cancel job in {Status} status.");

        Status = JobStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
    }

    // For EF Core
    private ProcessingJob()
        : base() { }
}
