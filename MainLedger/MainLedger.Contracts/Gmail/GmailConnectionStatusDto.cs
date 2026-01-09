namespace MainLedger.Contracts.Gmail;

/// <summary>
/// DTO for Gmail connection status.
/// </summary>
public record GmailConnectionStatusDto(
    bool IsConnected,
    string? Email,
    DateTime? LastSyncedAt,
    DateTime? ConnectedAt,
    int TotalEmailsSynced
);
