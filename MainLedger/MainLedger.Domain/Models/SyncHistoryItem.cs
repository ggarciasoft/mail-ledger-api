namespace MainLedger.Domain.Models;

/// <summary>
/// Model for sync history item.
/// </summary>
public class SyncHistoryItem
{
    public DateTime SyncedAt { get; set; }
    public int EmailCount { get; set; }
}
