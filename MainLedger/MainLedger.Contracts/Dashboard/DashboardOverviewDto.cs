namespace MainLedger.Contracts.Dashboard;

/// <summary>
/// Dashboard overview DTO with aggregated statistics.
/// </summary>
public class DashboardOverviewDto
{
    public int TotalEmails { get; init; }
    public int PendingClassification { get; init; }
    public int PendingExtraction { get; init; }
    public int PendingConfirmation { get; init; }
    public int ConfirmedRecords { get; init; }
    public int FailedProcessing { get; init; }
    public DateTime? LastSyncAt { get; init; }
    public List<RecentActivityDto> RecentActivity { get; init; } = new();
}

public class RecentActivityDto
{
    public string Type { get; init; } = string.Empty;
    public int Count { get; init; }
    public DateTime Timestamp { get; init; }
}
