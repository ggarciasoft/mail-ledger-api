using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using MainLedger.Application.Common.Interfaces;
using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using MainLedger.Domain.Services;
using MainLedger.Domain.Settings;
using MainLedger.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Client;
using Microsoft.Kiota.Abstractions.Authentication;

namespace MainLedger.Integrations.Services;

public class OutlookEmailProvider : IEmailProvider
{
    private readonly IEmailConnectionRepository _connectionRepository;
    private readonly IEmailMessageRepository _emailMessageRepository;
    private readonly ITokenEncryptionService _encryptionService;
    private readonly IPkceStateStore _pkceStateStore;
    private readonly IOAuthStateService _oauthStateService;
    private readonly OutlookSettings _settings;
    private readonly ILogger<OutlookEmailProvider> _logger;

    public EmailProvider ProviderType => EmailProvider.Outlook;

    public OutlookEmailProvider(
        IEmailConnectionRepository connectionRepository,
        IEmailMessageRepository emailMessageRepository,
        ITokenEncryptionService encryptionService,
        IPkceStateStore pkceStateStore,
        IOAuthStateService oauthStateService,
        OutlookSettings settings,
        ILogger<OutlookEmailProvider> logger
    )
    {
        _connectionRepository = connectionRepository;
        _emailMessageRepository = emailMessageRepository;
        _encryptionService = encryptionService;
        _pkceStateStore = pkceStateStore;
        _oauthStateService = oauthStateService;
        _settings = settings;
        _logger = logger;
    }

    public async Task<OAuthUrlResult> GetAuthorizationUrlAsync(Guid userId)
    {
        // Generate state with user ID encoded
        var state = _oauthStateService.GenerateState(userId);
        var scopes = string.Join(" ", _settings.Scopes);

        // Generate PKCE parameters
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);

        // Store code verifier for later use (expires in 10 minutes)
        await _pkceStateStore.StoreAsync(state, codeVerifier, TimeSpan.FromMinutes(10));

        var authUrl =
            $"https://login.microsoftonline.com/{_settings.TenantId}/oauth2/v2.0/authorize?"
            + $"client_id={_settings.ClientId}"
            + $"&response_type=code"
            + $"&redirect_uri={Uri.EscapeDataString(_settings.RedirectUri)}"
            + $"&scope={Uri.EscapeDataString(scopes)}"
            + $"&state={state}"
            + $"&code_challenge={codeChallenge}"
            + $"&code_challenge_method=S256"
            + $"&response_mode=query";

        return new OAuthUrlResult { AuthorizationUrl = authUrl, State = state };
    }

    public async Task<ConnectionResult> HandleOAuthCallbackAsync(string code, string state, Guid userId)
    {
        try
        {
            // Exchange code for tokens
            var tokenResponse = await ExchangeCodeForTokensAsync(code, state);

            // Get user email using access token
            var userEmail = await GetUserEmailAsync(tokenResponse.AccessToken);

            // Check if connection already exists
            var existingConnection = await _connectionRepository.GetByUserAndProviderAsync(
                userId,
                EmailProvider.Outlook
            );

            if (existingConnection != null)
            {
                // Update existing connection
                existingConnection.Email = userEmail;
                existingConnection.EncryptedAccessToken = _encryptionService.Encrypt(
                    tokenResponse.AccessToken
                );
                existingConnection.EncryptedRefreshToken = _encryptionService.Encrypt(
                    tokenResponse.RefreshToken
                );
                existingConnection.TokenExpiresAt = DateTime.UtcNow.AddSeconds(
                    tokenResponse.ExpiresIn
                );
                existingConnection.IsActive = true;
                existingConnection.ConnectedAt = DateTime.UtcNow;

                await _connectionRepository.UpdateAsync(existingConnection);
            }
            else
            {
                // Create new connection
                var connection = new EmailConnection(Guid.NewGuid())
                {
                    UserId = userId,
                    Provider = EmailProvider.Outlook,
                    Email = userEmail,
                    EncryptedAccessToken = _encryptionService.Encrypt(tokenResponse.AccessToken),
                    EncryptedRefreshToken = _encryptionService.Encrypt(tokenResponse.RefreshToken),
                    TokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn),
                    IsActive = true,
                    ConnectedAt = DateTime.UtcNow,
                };

                await _connectionRepository.AddAsync(connection);
            }

            return new ConnectionResult { Success = true, Email = userEmail };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to handle Outlook OAuth callback for user {UserId}",
                userId
            );
            return new ConnectionResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<SyncResult> SyncEmailsAsync(Guid userId, SyncOptions options)
    {
        var connection = await _connectionRepository.GetByUserAndProviderAsync(
            userId,
            EmailProvider.Outlook
        );
        if (connection == null || !connection.IsActive)
        {
            throw new InvalidOperationException("No active Outlook connection found");
        }

        var result = new SyncResult { Errors = new List<string>() };

        try
        {
            var accessToken = await GetValidAccessTokenAsync(connection);
            var graphClient = CreateGraphClient(accessToken);

            // Build filter
            var filter = BuildMailFilter(options);

            // Fetch emails from inbox
            var messages = await graphClient.Me.Messages.GetAsync(config =>
            {
                if (!string.IsNullOrEmpty(filter))
                    config.QueryParameters.Filter = filter;
                config.QueryParameters.Top = options.MaxResults ?? 100;
                config.QueryParameters.Orderby = new[] { "receivedDateTime desc" };
                config.QueryParameters.Select = new[]
                {
                    "id",
                    "subject",
                    "from",
                    "receivedDateTime",
                    "body",
                    "bodyPreview",
                };
            });

            if (messages?.Value == null)
            {
                return result;
            }

            var mailHashes = new HashSet<string>();

            foreach (var message in messages.Value)
            {
                try
                {
                    // Check if already synced
                    var existingEmail = await _emailMessageRepository.GetByProviderMessageIdAsync(
                        message.Id ?? string.Empty,
                        EmailProvider.Outlook
                    );

                    if (existingEmail != null)
                    {
                        result.EmailsSkipped++;
                        continue;
                    }

                    // Extract email body text
                    var bodyText = message.Body?.Content ?? message.BodyPreview ?? string.Empty;

                    // Create content hash
                    var contentHash = ComputeContentHash(message.Subject ?? "", bodyText);

                    // Check if email with same content already exists
                    var contentExists = await _emailMessageRepository.ExistsByContentHashAsync(contentHash);
                    if (contentExists || mailHashes.Contains(contentHash))
                    {
                        _logger.LogDebug("Skipping email {MessageId} - duplicate content hash", message.Id);
                        result.EmailsSkipped++;
                        continue;
                    }

                    // Create email message using factory method
                    var emailMessage = EmailMessage.Create(
                        messageId: message.Id ?? Guid.NewGuid().ToString(),
                        threadId: message.ConversationId ?? message.Id ?? Guid.NewGuid().ToString(),
                        userId: userId,
                        subject: message.Subject ?? "(No Subject)",
                        from: Domain.ValueObjects.EmailAddress.Create(
                            message.From?.EmailAddress?.Address ?? "unknown@unknown.com"
                        ),
                        receivedAt: message.ReceivedDateTime?.UtcDateTime ?? DateTime.UtcNow,
                        bodyText: bodyText,
                        contentHash: contentHash
                    );

                    await _emailMessageRepository.AddAsync(emailMessage);
                    result.EmailsSynced++;

                    mailHashes.Add(contentHash);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to sync Outlook email {MessageId}", message.Id);
                    result.Errors.Add($"Failed to sync email {message.Id}: {ex.Message}");
                }
            }

            connection.LastSyncedAt = DateTime.UtcNow;
            await _connectionRepository.UpdateAsync(connection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync Outlook emails for user {UserId}", userId);
            result.Errors.Add($"Sync failed: {ex.Message}");
        }

        return result;
    }

    public async Task<Domain.Services.ConnectionStatus> GetConnectionStatusAsync(Guid userId)
    {
        var connection = await _connectionRepository.GetByUserAndProviderAsync(
            userId,
            EmailProvider.Outlook
        );

        if (connection == null)
        {
            return new Domain.Services.ConnectionStatus
            {
                IsConnected = false,
                Email = string.Empty,
            };
        }

        return new Domain.Services.ConnectionStatus
        {
            IsConnected = connection.IsActive,
            Email = connection.Email,
            LastSyncedAt = connection.LastSyncedAt,
        };
    }

    public async Task DisconnectAsync(Guid userId)
    {
        var connection = await _connectionRepository.GetByUserAndProviderAsync(
            userId,
            EmailProvider.Outlook
        );
        if (connection != null)
        {
            connection.IsActive = false;
            await _connectionRepository.UpdateAsync(connection);
        }
    }

    private async Task<string> GetValidAccessTokenAsync(EmailConnection connection)
    {
        // Check if token is still valid (with 5-minute buffer)
        if (connection.TokenExpiresAt > DateTime.UtcNow.AddMinutes(5))
        {
            return _encryptionService.Decrypt(connection.EncryptedAccessToken);
        }

        // Token expired - mark connection as inactive and throw exception
        // User will need to reconnect their Outlook account
        _logger.LogWarning(
            "Outlook access token expired for user {UserId}. Connection marked as inactive.",
            connection.UserId
        );
        
        connection.IsActive = false;
        await _connectionRepository.UpdateAsync(connection);
        
        throw new InvalidOperationException(
            "Outlook access token has expired. Please reconnect your Outlook account in settings."
        );
    }

    private async Task<TokenResponse> ExchangeCodeForTokensAsync(string code, string state)
    {
        // Retrieve code verifier from storage
        var codeVerifier = await _pkceStateStore.RetrieveAsync(state);
        if (string.IsNullOrEmpty(codeVerifier))
        {
            throw new InvalidOperationException("Code verifier not found for the provided state. The OAuth flow may have expired.");
        }

        try
        {
            // Use ConfidentialClientApplication for server-side OAuth with client secret
            var app = ConfidentialClientApplicationBuilder
                .Create(_settings.ClientId)
                .WithClientSecret(_settings.ClientSecret)
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{_settings.TenantId}"))
                .WithRedirectUri(_settings.RedirectUri)
                .Build();

            var result = await app.AcquireTokenByAuthorizationCode(_settings.Scopes, code)
                .WithPkceCodeVerifier(codeVerifier)
                .ExecuteAsync();

            // For confidential clients, we need to extract the refresh token from the cache
            // Store the account identifier which we'll use to get tokens later
            var accountId = result.Account?.HomeAccountId?.Identifier ?? result.Account?.Username ?? string.Empty;
            
            return new TokenResponse
            {
                AccessToken = result.AccessToken,
                RefreshToken = accountId, // Store account identifier for now
                ExpiresIn = (int)(result.ExpiresOn - DateTimeOffset.UtcNow).TotalSeconds,
            };
        }
        finally
        {
            // Clean up code verifier after use
            await _pkceStateStore.RemoveAsync(state);
        }
    }

    private async Task<TokenResponse> RefreshAccessTokenAsync(string accountIdentifier)
    {
        // For now, just throw an exception indicating re-authentication is needed
        // MSAL's token cache doesn't persist between requests for confidential clients
        // A proper solution would require implementing a persistent token cache
        // or storing the refresh token directly and using the token endpoint
        
        _logger.LogWarning("Token refresh not supported - user needs to reconnect Outlook");
        throw new InvalidOperationException(
            "Access token expired. Please reconnect your Outlook account."
        );
    }

    private async Task<string> GetUserEmailAsync(string accessToken)
    {
        var graphClient = CreateGraphClient(accessToken);
        var user = await graphClient.Me.GetAsync();
        return user?.Mail ?? user?.UserPrincipalName ?? "unknown@unknown.com";
    }

    private GraphServiceClient CreateGraphClient(string accessToken)
    {
        var authProvider = new BaseBearerTokenAuthenticationProvider(
            new TokenProvider(accessToken)
        );
        return new GraphServiceClient(authProvider);
    }

    private string BuildMailFilter(SyncOptions options)
    {
        var filters = new List<string>();

        if (options.SyncFrom.HasValue)
        {
            filters.Add($"receivedDateTime ge {options.SyncFrom.Value:yyyy-MM-ddTHH:mm:ssZ}");
        }

        return filters.Count > 0 ? string.Join(" and ", filters) : string.Empty;
    }

    private string ComputeContentHash(string subject, string body)
    {
        var content = $"{subject}|{body}";
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Generate a cryptographically random code verifier for PKCE (128 characters)
    /// </summary>
    private static string GenerateCodeVerifier()
    {
        var bytes = new byte[96]; // 96 bytes = 128 base64url characters
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncode(bytes);
    }

    /// <summary>
    /// Generate code challenge from code verifier using SHA256
    /// </summary>
    private static string GenerateCodeChallenge(string codeVerifier)
    {
        var bytes = Encoding.UTF8.GetBytes(codeVerifier);
        var hash = SHA256.HashData(bytes);
        return Base64UrlEncode(hash);
    }

    /// <summary>
    /// Base64url encoding (RFC 4648 Section 5)
    /// </summary>
    private static string Base64UrlEncode(byte[] input)
    {
        var base64 = Convert.ToBase64String(input);
        return base64
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    private class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
    }

    private class TokenProvider : IAccessTokenProvider
    {
        private readonly string _accessToken;

        public TokenProvider(string accessToken)
        {
            _accessToken = accessToken;
        }

        public Task<string> GetAuthorizationTokenAsync(
            Uri uri,
            Dictionary<string, object>? additionalAuthenticationContext = null,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(_accessToken);
        }

        public AllowedHostsValidator AllowedHostsValidator => new AllowedHostsValidator();
    }
}
