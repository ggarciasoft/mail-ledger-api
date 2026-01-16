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
    private readonly OutlookSettings _settings;
    private readonly ILogger<OutlookEmailProvider> _logger;

    public EmailProvider ProviderType => EmailProvider.Outlook;

    public OutlookEmailProvider(
        IEmailConnectionRepository connectionRepository,
        IEmailMessageRepository emailMessageRepository,
        ITokenEncryptionService encryptionService,
        OutlookSettings settings,
        ILogger<OutlookEmailProvider> logger
    )
    {
        _connectionRepository = connectionRepository;
        _emailMessageRepository = emailMessageRepository;
        _encryptionService = encryptionService;
        _settings = settings;
        _logger = logger;
    }

    public Task<OAuthUrlResult> GetAuthorizationUrlAsync(Guid userId)
    {
        var state = Guid.NewGuid().ToString();
        var scopes = string.Join(" ", _settings.Scopes);

        var authUrl =
            $"https://login.microsoftonline.com/{_settings.TenantId}/oauth2/v2.0/authorize?"
            + $"client_id={_settings.ClientId}"
            + $"&response_type=code"
            + $"&redirect_uri={Uri.EscapeDataString(_settings.RedirectUri)}"
            + $"&scope={Uri.EscapeDataString(scopes)}"
            + $"&state={state}"
            + $"&response_mode=query";

        return Task.FromResult(new OAuthUrlResult { AuthorizationUrl = authUrl, State = state });
    }

    public async Task<ConnectionResult> HandleOAuthCallbackAsync(string code, Guid userId)
    {
        try
        {
            // Exchange code for tokens
            var tokenResponse = await ExchangeCodeForTokensAsync(code);

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

        // Refresh token
        var refreshToken = _encryptionService.Decrypt(connection.EncryptedRefreshToken);
        var tokenResponse = await RefreshAccessTokenAsync(refreshToken);

        connection.EncryptedAccessToken = _encryptionService.Encrypt(tokenResponse.AccessToken);
        connection.EncryptedRefreshToken = _encryptionService.Encrypt(tokenResponse.RefreshToken);
        connection.TokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
        await _connectionRepository.UpdateAsync(connection);

        return tokenResponse.AccessToken;
    }

    private async Task<TokenResponse> ExchangeCodeForTokensAsync(string code)
    {
        var app = ConfidentialClientApplicationBuilder
            .Create(_settings.ClientId)
            .WithClientSecret(_settings.ClientSecret)
            .WithAuthority(new Uri($"https://login.microsoftonline.com/{_settings.TenantId}"))
            .WithRedirectUri(_settings.RedirectUri)
            .Build();

        var result = await app.AcquireTokenByAuthorizationCode(_settings.Scopes, code)
            .ExecuteAsync();

        return new TokenResponse
        {
            AccessToken = result.AccessToken,
            RefreshToken = result.Account?.HomeAccountId?.Identifier ?? string.Empty,
            ExpiresIn = (int)(result.ExpiresOn - DateTimeOffset.UtcNow).TotalSeconds,
        };
    }

    private async Task<TokenResponse> RefreshAccessTokenAsync(string accountIdentifier)
    {
        var app = ConfidentialClientApplicationBuilder
            .Create(_settings.ClientId)
            .WithClientSecret(_settings.ClientSecret)
            .WithAuthority(new Uri($"https://login.microsoftonline.com/{_settings.TenantId}"))
            .Build();

        // Try to get the account by identifier
        if (!string.IsNullOrEmpty(accountIdentifier))
        {
            try
            {
                var account = await app.GetAccountAsync(accountIdentifier);
                if (account != null)
                {
                    var result = await app.AcquireTokenSilent(_settings.Scopes, account)
                        .ExecuteAsync();

                    return new TokenResponse
                    {
                        AccessToken = result.AccessToken,
                        RefreshToken = accountIdentifier, // Keep existing account identifier
                        ExpiresIn = (int)(result.ExpiresOn - DateTimeOffset.UtcNow).TotalSeconds,
                    };
                }
            }
            catch (MsalUiRequiredException)
            {
                // Silent token acquisition failed, user needs to re-authenticate
                throw new InvalidOperationException(
                    "Token refresh failed. User needs to re-authenticate."
                );
            }
        }

        throw new InvalidOperationException("No valid account identifier found for token refresh");
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
