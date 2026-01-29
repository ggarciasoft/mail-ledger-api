using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Hangfire;
using MainLedger.Application.BackgroundJobs;
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
    private readonly IProcessingJobRepository _jobRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly OutlookSettings _settings;
    private readonly ILogger<OutlookEmailProvider> _logger;

    public EmailProvider ProviderType => EmailProvider.Outlook;

    public OutlookEmailProvider(
        IEmailConnectionRepository connectionRepository,
        IEmailMessageRepository emailMessageRepository,
        ITokenEncryptionService encryptionService,
        IPkceStateStore pkceStateStore,
        IOAuthStateService oauthStateService,
        IProcessingJobRepository jobRepository,
        IUnitOfWork unitOfWork,
        OutlookSettings settings,
        ILogger<OutlookEmailProvider> logger
    )
    {
        _connectionRepository = connectionRepository;
        _emailMessageRepository = emailMessageRepository;
        _encryptionService = encryptionService;
        _pkceStateStore = pkceStateStore;
        _oauthStateService = oauthStateService;
        _jobRepository = jobRepository;
        _unitOfWork = unitOfWork;
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
        try
        {
            _logger.LogInformation("Enqueueing Outlook sync job for user {UserId}", userId);

            var connection = await _connectionRepository.GetByUserAndProviderAsync(
                userId,
                EmailProvider.Outlook
            );
            
            if (connection == null || !connection.IsActive)
            {
                _logger.LogWarning("No active Outlook connection found for user {UserId}", userId);
                return new SyncResult
                {
                    EmailsSynced = 0,
                    EmailsSkipped = 0,
                    Errors = new List<string>
                    {
                        "No active Outlook connection found. Please connect your Outlook account first.",
                    },
                };
            }

            // Create a processing job for tracking
            var job = ProcessingJob.Create(
                userId,
                JobType.EmailSync,
                string.Empty, // Hangfire job ID will be set after enqueueing
                $"Provider: Outlook, MaxEmails: {options.MaxResults ?? 100}"
            );

            await _jobRepository.AddAsync(job, CancellationToken.None);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);

            // Enqueue background job using Hangfire
            var hangfireJobId = BackgroundJob.Enqueue<EmailSyncBackgroundJob>(x =>
                x.ExecuteAsync(job.Id, userId, EmailProvider.Outlook, options.MaxResults ?? 100, default)
            );

            job.SetHangfireJobId(hangfireJobId);
            _jobRepository.Update(job);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);

            _logger.LogInformation(
                "Outlook sync job {JobId} enqueued with Hangfire ID {HangfireJobId}",
                job.Id,
                hangfireJobId
            );

            // Return immediately - the background job will handle the actual sync
            // Frontend should poll for job status or use SignalR for real-time updates
            return new SyncResult
            {
                EmailsSynced = 0,
                EmailsSkipped = 0,
                Errors = new List<string>(), // Empty errors - job queued successfully
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue Outlook sync job for user {UserId}", userId);
            return new SyncResult
            {
                EmailsSynced = 0,
                EmailsSkipped = 0,
                Errors = new List<string> { $"Failed to queue sync job: {ex.Message}" },
            };
        }
    }

    /// <summary>
    /// Internal method to perform the actual Outlook email sync.
    /// Called by EmailSyncBackgroundJob.
    /// </summary>
    public async Task<SyncResult> PerformSyncAsync(Guid userId, SyncOptions options, CancellationToken cancellationToken = default)
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
                    "internetMessageId",
                    "conversationId",
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
                    // Validate message ID - this should never be null if we're selecting it
                    if (string.IsNullOrEmpty(message.Id))
                    {
                        _logger.LogWarning("Skipping message with null ID. Subject: {Subject}", message.Subject);
                        result.EmailsSkipped++;
                        continue;
                    }

                    // Check if already synced by provider message ID
                    var existingEmail = await _emailMessageRepository.GetByProviderMessageIdAsync(
                        message.Id,
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
                        messageId: message.Id,
                        threadId: message.ConversationId ?? message.Id,
                        userId: userId,
                        provider: EmailProvider.Outlook,
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
            
            _logger.LogInformation(
                "Outlook sync completed for user {UserId}. Synced: {Synced}, Skipped: {Skipped}, Errors: {ErrorCount}",
                userId,
                result.EmailsSynced,
                result.EmailsSkipped,
                result.Errors.Count
            );
            
            // Commit all changes to database
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);
            
            _logger.LogInformation("Database changes committed successfully for user {UserId}", userId);
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

        // Token expired - attempt to refresh it
        _logger.LogInformation(
            "Outlook access token expired for user {UserId}. Attempting refresh...",
            connection.UserId
        );

        try
        {
            // Decrypt the refresh token
            var refreshToken = _encryptionService.Decrypt(connection.EncryptedRefreshToken);

            // Call the token endpoint to refresh
            var tokenResponse = await RefreshAccessTokenAsync(refreshToken);

            // Encrypt and update the connection with new tokens
            connection.EncryptedAccessToken = _encryptionService.Encrypt(tokenResponse.AccessToken);
            connection.EncryptedRefreshToken = _encryptionService.Encrypt(tokenResponse.RefreshToken);
            connection.TokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

            await _connectionRepository.UpdateAsync(connection);

            _logger.LogInformation(
                "Successfully refreshed Outlook access token for user {UserId}",
                connection.UserId
            );

            return tokenResponse.AccessToken;
        }
        catch (Exception ex)
        {
            // Refresh failed - mark connection as inactive
            _logger.LogError(
                ex,
                "Failed to refresh Outlook token for user {UserId}. Marking connection inactive.",
                connection.UserId
            );

            connection.IsActive = false;
            await _connectionRepository.UpdateAsync(connection);

            throw new InvalidOperationException(
                "Outlook access token has expired and refresh failed. Please reconnect your Outlook account in settings.",
                ex
            );
        }
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
            // Use HttpClient to call Microsoft Identity token endpoint directly
            // This allows us to get the refresh token, which MSAL doesn't expose for confidential clients
            using var httpClient = new HttpClient();
            
            var tokenEndpoint = $"https://login.microsoftonline.com/{_settings.TenantId}/oauth2/v2.0/token";
            
            var requestBody = new Dictionary<string, string>
            {
                { "client_id", _settings.ClientId },
                { "client_secret", _settings.ClientSecret },
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", _settings.RedirectUri },
                { "code_verifier", codeVerifier },
                { "scope", string.Join(" ", _settings.Scopes) }
            };

            var response = await httpClient.PostAsync(
                tokenEndpoint,
                new FormUrlEncodedContent(requestBody)
            );

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "Token exchange failed with status {StatusCode}: {Error}",
                    response.StatusCode,
                    errorContent
                );
                throw new InvalidOperationException(
                    $"Failed to exchange authorization code for tokens: {response.StatusCode}"
                );
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenData = System.Text.Json.JsonDocument.Parse(responseContent);
            
            var accessToken = tokenData.RootElement.GetProperty("access_token").GetString()
                ?? throw new InvalidOperationException("No access token in response");
            
            var refreshToken = tokenData.RootElement.GetProperty("refresh_token").GetString()
                ?? throw new InvalidOperationException("No refresh token in response");
            
            var expiresIn = tokenData.RootElement.GetProperty("expires_in").GetInt32();

            _logger.LogInformation("Successfully exchanged authorization code for tokens");

            return new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = expiresIn
            };
        }
        finally
        {
            // Clean up code verifier after use
            await _pkceStateStore.RemoveAsync(state);
        }
    }

    private async Task<TokenResponse> RefreshAccessTokenAsync(string refreshToken)
    {
        _logger.LogInformation("Refreshing Outlook access token using refresh token");

        try
        {
            // Use HttpClient to call Microsoft Identity token endpoint directly
            using var httpClient = new HttpClient();
            
            var tokenEndpoint = $"https://login.microsoftonline.com/{_settings.TenantId}/oauth2/v2.0/token";
            
            var requestBody = new Dictionary<string, string>
            {
                { "client_id", _settings.ClientId },
                { "client_secret", _settings.ClientSecret },
                { "grant_type", "refresh_token" },
                { "refresh_token", refreshToken },
                { "scope", string.Join(" ", _settings.Scopes) }
            };

            var response = await httpClient.PostAsync(
                tokenEndpoint,
                new FormUrlEncodedContent(requestBody)
            );

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "Token refresh failed with status {StatusCode}: {Error}",
                    response.StatusCode,
                    errorContent
                );
                throw new InvalidOperationException(
                    $"Failed to refresh access token: {response.StatusCode}"
                );
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenData = System.Text.Json.JsonDocument.Parse(responseContent);
            
            var accessToken = tokenData.RootElement.GetProperty("access_token").GetString()
                ?? throw new InvalidOperationException("No access token in refresh response");
            
            var expiresIn = tokenData.RootElement.GetProperty("expires_in").GetInt32();
            
            // Refresh token may or may not be returned - if not, keep using the old one
            var newRefreshToken = tokenData.RootElement.TryGetProperty("refresh_token", out var refreshProp)
                ? refreshProp.GetString()
                : refreshToken;

            _logger.LogInformation("Successfully refreshed Outlook access token");

            return new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken ?? refreshToken,
                ExpiresIn = expiresIn
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh Outlook access token");
            throw new InvalidOperationException(
                "Failed to refresh access token. Please reconnect your Outlook account.",
                ex
            );
        }
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
