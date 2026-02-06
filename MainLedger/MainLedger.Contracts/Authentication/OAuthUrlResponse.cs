namespace MainLedger.Contracts.Authentication;

/// <summary>
/// Request to get OAuth authorization URL.
/// </summary>
public record OAuthUrlResponse(
    string Url,
    string State
);
