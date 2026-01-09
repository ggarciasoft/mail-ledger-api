namespace MainLedger.Contracts.Gmail;

/// <summary>
/// DTO for sync history item.
/// </summary>
public record SyncHistoryItemDto(
    DateTime SyncedAt,
    int EmailsProcessed,
    string Status
);

/// <summary>
/// DTO for sync history response.
/// </summary>
public record SyncHistoryDto(
    List<SyncHistoryItemDto> History,
    DateTime? LastSuccessfulSync,
    int TotalSyncs
);
