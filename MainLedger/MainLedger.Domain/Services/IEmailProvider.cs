using MainLedger.Domain.Enums;

namespace MainLedger.Domain.Services;

public interface IEmailProvider
{
    EmailProvider ProviderType { get; }
    Task<OAuthUrlResult> GetAuthorizationUrlAsync(Guid userId);
    Task<ConnectionResult> HandleOAuthCallbackAsync(string code, Guid userId);
    Task<SyncResult> SyncEmailsAsync(Guid userId, SyncOptions options);
    Task<ConnectionStatus> GetConnectionStatusAsync(Guid userId);
    Task DisconnectAsync(Guid userId);
}

public class OAuthUrlResult
{
    public string AuthorizationUrl { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}

public class ConnectionResult
{
    public bool Success { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}

public class SyncResult
{
    public int EmailsSynced { get; set; }
    public int EmailsSkipped { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class ConnectionStatus
{
    public bool IsConnected { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime? LastSyncedAt { get; set; }
}

public class SyncOptions
{
    public DateTime? SyncFrom { get; set; }
    public int? MaxResults { get; set; }
}
