namespace MainLedger.Application.Common.Interfaces;

/// <summary>
/// Service for temporarily storing PKCE code verifiers during OAuth flow
/// </summary>
public interface IPkceStateStore
{
    /// <summary>
    /// Store a code verifier associated with an OAuth state parameter
    /// </summary>
    /// <param name="state">OAuth state parameter</param>
    /// <param name="codeVerifier">PKCE code verifier</param>
    /// <param name="expiration">How long to store the verifier</param>
    Task StoreAsync(string state, string codeVerifier, TimeSpan expiration);

    /// <summary>
    /// Retrieve a code verifier by state parameter
    /// </summary>
    /// <param name="state">OAuth state parameter</param>
    /// <returns>Code verifier if found, null otherwise</returns>
    Task<string?> RetrieveAsync(string state);

    /// <summary>
    /// Remove a code verifier from storage
    /// </summary>
    /// <param name="state">OAuth state parameter</param>
    Task RemoveAsync(string state);
}
