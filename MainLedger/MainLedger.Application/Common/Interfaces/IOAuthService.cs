using MainLedger.Domain.Settings;

namespace MainLedger.Application.Common.Interfaces;

/// <summary>
/// Service for OAuth authentication operations.
/// </summary>
public interface IOAuthService
{
    /// <summary>
    /// Generates the authorization URL for the specified OAuth provider.
    /// </summary>
    /// <param name="provider">OAuth provider (Google or Microsoft)</param>
    /// <param name="state">State parameter for CSRF protection</param>
    /// <returns>Authorization URL</returns>
    Task<string> GetAuthorizationUrlAsync(string provider, string state);

    /// <summary>
    /// Exchanges an authorization code for user information.
    /// </summary>
    /// <param name="provider">OAuth provider (Google or Microsoft)</param>
    /// <param name="code">Authorization code from OAuth callback</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User information from the OAuth provider</returns>
    Task<OAuthUserInfo> ExchangeCodeForUserInfoAsync(
        string provider,
        string code,
        CancellationToken cancellationToken = default
    );
}
