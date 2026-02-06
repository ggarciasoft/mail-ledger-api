namespace MainLedger.Contracts.Authentication;

/// <summary>
/// Request for OAuth callback with authorization code.
/// </summary>
public record OAuthCallbackRequest(
    string Code
);
