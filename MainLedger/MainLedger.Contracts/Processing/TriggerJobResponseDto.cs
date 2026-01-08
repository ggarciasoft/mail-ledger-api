namespace MainLedger.Contracts.Processing;

/// <summary>
/// Response DTO for job trigger operations.
/// </summary>
public class TriggerJobResponseDto
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int ProcessedCount { get; init; }
    public int SucceededCount { get; init; }
    public int FailedCount { get; init; }
}
