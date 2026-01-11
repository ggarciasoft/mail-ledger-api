namespace MainLedger.Contracts.Jobs;

/// <summary>
/// DTO for processing job data sent via SignalR.
/// </summary>
public class JobDto
{
    public required string JobId { get; init; }
    public required string UserId { get; init; }
    public required string JobType { get; init; }
    public required string Status { get; init; }
    public double Progress { get; init; }
    public int TotalItems { get; init; }
    public int ProcessedItems { get; init; }
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}
