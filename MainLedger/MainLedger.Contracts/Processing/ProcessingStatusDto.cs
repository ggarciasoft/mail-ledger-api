namespace MainLedger.Contracts.Processing;

/// <summary>
/// Processing status DTO with job information.
/// </summary>
public class ProcessingStatusDto
{
    public int PendingClassification { get; init; }
    public int PendingExtraction { get; init; }
    public bool CanClassify { get; init; }
    public bool CanExtract { get; init; }
    public JobStatusDto? LastClassificationJob { get; init; }
    public JobStatusDto? LastExtractionJob { get; init; }
}

public class JobStatusDto
{
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public int Processed { get; init; }
    public int Succeeded { get; init; }
    public int Failed { get; init; }
}
