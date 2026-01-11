namespace MainLedger.Domain.Enums;

/// <summary>
/// Status of a background processing job.
/// </summary>
public enum JobStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled,
}
