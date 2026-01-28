namespace MainLedger.Contracts.Email;

public record EmailConnectionDto
{
    public string Provider { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool IsConnected { get; init; }
    public DateTime? LastSyncedAt { get; init; }
    public DateTime ConnectedAt { get; init; }
}

public record GetAuthUrlResponse
{
    public string AuthorizationUrl { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
}

public record ConnectProviderRequest
{
    public string Code { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
}

public record SyncEmailsRequest
{
    public DateTime? SyncFrom { get; init; }
    public int? MaxResults { get; init; }
}

public record SyncResultDto
{
    public int EmailsSynced { get; init; }
    public int EmailsSkipped { get; init; }
    public List<string> Errors { get; init; } = new();
}
