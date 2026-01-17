using MainLedger.Domain.Entities;

namespace MainLedger.Application.Common.Interfaces;

/// <summary>
/// Interface for Gmail integration services.
/// Handles OAuth flow and email operations.
/// </summary>
public interface IGmailService
{
    /// <summary>
    /// Generates the OAuth authorization URL for the user to grant permissions.
    /// </summary>
    /// <param name="userId">The ID of the user initiating the connection.</param>
    /// <returns>The authorization URL.</returns>
    string GetAuthorizationUrl(Guid userId);

    /// <summary>
    /// Handles the OAuth callback code, exchanges it for tokens, and creates/updates the GmailConnection.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="code">The authorization code returned by Google.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created or updated GmailConnection.</returns>
    Task<EmailConnection> HandleCallbackAsync(
        Guid userId,
        string code,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Refreshes the access token for an existing connection.
    /// </summary>
    /// <param name="connection">The Gmail connection to refresh.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RefreshTokenAsync(EmailConnection connection, CancellationToken cancellationToken);

    /// <summary>
    /// Fetches new emails from Gmail.
    /// </summary>
    /// <param name="connection">The Gmail connection to use.</param>
    /// <param name="rules">User-defined rules for filtering emails (optional).</param>
    /// <param name="processFrom">Only fetch emails after this date (optional).</param>
    /// <param name="historyId">The history ID to sync from (optional, for partial sync).</param>
    /// <param name="maxResults">Maximum number of emails to fetch (default 50).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of fetched email messages (converted to domain entities).</returns>
    Task<List<EmailMessage>> FetchEmailsAsync(
        EmailConnection connection,
        List<Rule>? rules = null,
        DateTime? processFrom = null,
        string? historyId = null,
        int maxResults = 50,
        CancellationToken cancellationToken = default
    );
}
