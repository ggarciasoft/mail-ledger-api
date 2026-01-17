using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using MainLedger.Application.Common.Interfaces;
using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using MainLedger.Domain.Settings;
using MainLedger.Domain.ValueObjects;
using Microsoft.Extensions.Options;

namespace MainLedger.Integrations.Services;

/// <summary>
/// Implementation of Gmail integration using Google Client Library.
/// </summary>
public class GmailService : IGmailService
{
    private readonly GmailSettings _settings;
    private readonly IEmailConnectionRepository _emailConnectionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenEncryptionService _tokenEncryption;

    public GmailService(
        IOptions<GmailSettings> settings,
        IEmailConnectionRepository emailConnectionRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ITokenEncryptionService tokenEncryption
    )
    {
        _settings = settings.Value;
        _emailConnectionRepository = emailConnectionRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _tokenEncryption = tokenEncryption;
    }

    public string GetAuthorizationUrl(Guid userId)
    {
        var flow = new GoogleAuthorizationCodeFlow(
            new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = _settings.ClientId,
                    ClientSecret = _settings.ClientSecret,
                },
                Scopes = new[] { Google.Apis.Gmail.v1.GmailService.Scope.GmailReadonly },
                DataStore = null, // We manage storage manually
            }
        );

        // Build authorization URL manually to have full control over parameters
        var authorizationUrl = "https://accounts.google.com/o/oauth2/v2/auth";
        var parameters = new Dictionary<string, string>
        {
            { "client_id", _settings.ClientId },
            { "redirect_uri", _settings.RedirectUri },
            { "response_type", "code" },
            { "scope", Google.Apis.Gmail.v1.GmailService.Scope.GmailReadonly },
            { "access_type", "offline" }, // Request refresh token
            { "prompt", "consent" }, // Force consent screen
            { "state", userId.ToString() }, // Pass userId for callback correlation
        };

        var queryString = string.Join(
            "&",
            parameters.Select(kvp =>
                $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"
            )
        );

        return $"{authorizationUrl}?{queryString}";
    }

    public async Task<EmailConnection> HandleCallbackAsync(
        Guid userId,
        string code,
        CancellationToken cancellationToken
    )
    {
        var flow = new GoogleAuthorizationCodeFlow(
            new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = _settings.ClientId,
                    ClientSecret = _settings.ClientSecret,
                },
                Scopes = new[] { Google.Apis.Gmail.v1.GmailService.Scope.GmailReadonly },
            }
        );

        // Exchange code for token
        var tokenResponse = await flow.ExchangeCodeForTokenAsync(
            userId.ToString(),
            code,
            _settings.RedirectUri,
            cancellationToken
        );

        // Get user
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException($"User not found: {userId}");
        }

        // Get user's email from Gmail profile
        var credential = new UserCredential(flow, userId.ToString(), tokenResponse);
        var gmailService = new Google.Apis.Gmail.v1.GmailService(
            new Google.Apis.Services.BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
            }
        );

        var profile = await gmailService.Users.GetProfile("me").ExecuteAsync(cancellationToken);

        // Encrypt the refresh token before storing
        var refreshToken =
            tokenResponse.RefreshToken
            ?? throw new InvalidOperationException(
                "No refresh token received. Please ensure offline access is granted."
            );
        var encryptedToken = _tokenEncryption.Encrypt(refreshToken);

        // Check if EmailConnection already exists
        var existingEmailConnection = await _emailConnectionRepository.GetByUserAndProviderAsync(
            userId,
            EmailProvider.Gmail
        );

        if (existingEmailConnection != null)
        {
            // Update existing EmailConnection
            existingEmailConnection.Email = profile.EmailAddress;
            existingEmailConnection.EncryptedRefreshToken = encryptedToken;
            existingEmailConnection.EncryptedAccessToken = string.Empty; // Gmail doesn't use access tokens the same way
            existingEmailConnection.TokenExpiresAt = DateTime.UtcNow.AddYears(1); // Gmail refresh tokens don't expire
            existingEmailConnection.IsActive = true;
            existingEmailConnection.ConnectedAt = DateTime.UtcNow;

            await _emailConnectionRepository.UpdateAsync(existingEmailConnection);
        }
        else
        {
            // Create new EmailConnection
            var emailConnection = new EmailConnection(Guid.NewGuid())
            {
                UserId = userId,
                Provider = EmailProvider.Gmail,
                Email = profile.EmailAddress,
                EncryptedAccessToken = string.Empty, // Gmail doesn't use access tokens the same way
                EncryptedRefreshToken = encryptedToken,
                TokenExpiresAt = DateTime.UtcNow.AddYears(1), // Gmail refresh tokens don't expire
                IsActive = true,
                ConnectedAt = DateTime.UtcNow,
            };

            await _emailConnectionRepository.AddAsync(emailConnection);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Return the EmailConnection (either existing or newly created)
        var finalConnection =
            existingEmailConnection
            ?? await _emailConnectionRepository.GetByUserAndProviderAsync(
                userId,
                EmailProvider.Gmail
            );

        return finalConnection
            ?? throw new InvalidOperationException("Failed to create or retrieve email connection");
    }

    public async Task RefreshTokenAsync(
        EmailConnection connection,
        CancellationToken cancellationToken
    )
    {
        // To refresh, we just need to ensure the credential can get a new access token
        // The Google library handles this automatically if the refresh token is valid
        var service = await GetGoogleGmailServiceAsync(connection, cancellationToken);
        // We could force a token refresh here if needed, but usually implicit is enough
        await Task.CompletedTask;
    }

    public async Task<List<EmailMessage>> FetchEmailsAsync(
        EmailConnection connection,
        List<Rule>? rules = null,
        DateTime? processFrom = null,
        string? historyId = null,
        int maxResults = 50,
        CancellationToken cancellationToken = default
    )
    {
        var service = await GetGoogleGmailServiceAsync(connection, cancellationToken);

        var request = service.Users.Messages.List("me");
        request.MaxResults = maxResults;

        // Build Gmail query from rules and filters
        var queryParts = new List<string>();

        // Date filter
        if (processFrom.HasValue)
        {
            // Gmail query format: after:YYYY/MM/DD or after:timestamp
            // Using unix timestamp is safer for precision
            var unixTime = ((DateTimeOffset)processFrom.Value).ToUnixTimeSeconds();
            queryParts.Add($"after:{unixTime}");
        }

        // Apply user rules to Gmail query (pre-filter at Gmail API level for efficiency)
        if (rules != null && rules.Any())
        {
            var ruleQuery = BuildGmailQueryFromRules(rules);
            if (!string.IsNullOrWhiteSpace(ruleQuery))
            {
                queryParts.Add($"({ruleQuery})");
            }
        }

        if (queryParts.Count > 0)
        {
            request.Q = string.Join(" ", queryParts);
        }

        var response = await request.ExecuteAsync(cancellationToken);
        var emails = new List<EmailMessage>();

        if (response.Messages == null || response.Messages.Count == 0)
        {
            return emails;
        }

        foreach (var msgHeader in response.Messages)
        {
            var msgRequest = service.Users.Messages.Get("me", msgHeader.Id);
            msgRequest.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;
            var fullMsg = await msgRequest.ExecuteAsync(cancellationToken);

            // Parse headers
            var headers = fullMsg.Payload.Headers;
            var subject = headers.FirstOrDefault(h => h.Name == "Subject")?.Value ?? "(No Subject)";
            var fromRaw = headers.FirstOrDefault(h => h.Name == "From")?.Value ?? string.Empty;
            var dateRaw = headers.FirstOrDefault(h => h.Name == "Date")?.Value;

            // Parse Date
            if (!DateTime.TryParse(dateRaw, out var receivedAt))
            {
                // Fallback to internal date
                receivedAt = DateTimeOffset
                    .FromUnixTimeMilliseconds(fullMsg.InternalDate ?? 0)
                    .UtcDateTime;
            }

            // Parse Body
            var body = GetBody(fullMsg.Payload);

            // Create Domain Entity
            // Note: We need to parse 'From' carefully: "Name <email@example.com>"
            // For now, simpler parsing or trust EmailAddress value object to handle it
            // (EmailAddress expects clean email, so we need to extract it)
            var cleanEmail = ExtractEmail(fromRaw);

            // Compute hash for deduplication
            var contentHash = ComputeContentHash(fullMsg.Id, body);

            try
            {
                var emailEntity = EmailMessage.Create(
                    fullMsg.Id,
                    fullMsg.ThreadId,
                    connection.UserId,
                    subject,
                    EmailAddress.Create(cleanEmail),
                    receivedAt.ToUniversalTime(),
                    body,
                    contentHash
                );

                emails.Add(emailEntity);
            }
            catch (Exception ex)
            {
                // Log error but continue processing other emails
                // TODO: Add logging
                Console.WriteLine($"Failed to parse email {fullMsg.Id}: {ex.Message}");
            }
        }

        // Apply secondary rule-based filtering for complex patterns
        // Gmail API query is a pre-filter; this is the exact filter
        if (rules != null && rules.Any())
        {
            emails = ApplyRuleFiltering(emails, rules);
        }

        return emails;
    }

    // --- Private Helpers ---

    private async Task<Google.Apis.Gmail.v1.GmailService> GetGoogleGmailServiceAsync(
        EmailConnection connection,
        CancellationToken cancellationToken
    )
    {
        var flow = new GoogleAuthorizationCodeFlow(
            new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = _settings.ClientId,
                    ClientSecret = _settings.ClientSecret,
                },
                Scopes = new[] { Google.Apis.Gmail.v1.GmailService.Scope.GmailReadonly },
            }
        );

        // Decrypt the stored refresh token
        var decryptedRefreshToken = _tokenEncryption.Decrypt(connection.EncryptedRefreshToken);

        // Reconstruct token response from stored refresh token
        // Access token is null, so it will force a refresh
        var tokenResponse = new TokenResponse
        {
            RefreshToken = decryptedRefreshToken,
            ExpiresInSeconds = 0,
        };

        var credential = new UserCredential(flow, connection.UserId.ToString(), tokenResponse);

        // Ensure we can get an access token (verifies refresh token is still valid)
        // If this fails, the connection might be revoked
        try
        {
            await credential.RefreshTokenAsync(cancellationToken);
        }
        catch
        {
            // Token might be invalid/revoked
            // In a real app we might mark connection as inactive here
            throw new UnauthorizedAccessException(
                "Failed to refresh Gmail token. Connection may need re-authorization."
            );
        }

        return new Google.Apis.Gmail.v1.GmailService(
            new Google.Apis.Services.BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "MailLedger",
            }
        );
    }

    private string GetBody(MessagePart payload)
    {
        if (payload.Body != null && !string.IsNullOrEmpty(payload.Body.Data))
        {
            return DecodeBase64Url(payload.Body.Data);
        }

        if (payload.Parts != null && payload.Parts.Count > 0)
        {
            // Prefer text/plain
            var plainText = payload.Parts.FirstOrDefault(p => p.MimeType == "text/plain");
            if (plainText != null)
            {
                return GetBody(plainText);
            }

            // Fallback to any part
            return GetBody(payload.Parts[0]);
        }

        return string.Empty;
    }

    private string DecodeBase64Url(string input)
    {
        var base64 = input.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2:
                base64 += "==";
                break;
            case 3:
                base64 += "=";
                break;
        }
        var bytes = Convert.FromBase64String(base64);
        return System.Text.Encoding.UTF8.GetString(bytes);
    }

    private string ExtractEmail(string fromHeader)
    {
        // Format: "Name <email@example.com>" or "email@example.com"
        var start = fromHeader.IndexOf('<');
        var end = fromHeader.LastIndexOf('>');

        if (start >= 0 && end > start)
        {
            return fromHeader.Substring(start + 1, end - start - 1);
        }

        return fromHeader.Trim();
    }

    private string ComputeContentHash(string messageId, string body)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        // Hash combination of stable ID and content to detect changes if we re-fetch
        var input = $"{messageId}:{body}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// Builds a Gmail query string from user-defined rules.
    /// This allows pre-filtering at the Gmail API level for better performance.
    /// </summary>
    private string BuildGmailQueryFromRules(List<Rule> rules)
    {
        if (rules == null || !rules.Any(r => r.IsActive))
            return string.Empty;

        var activeRules = rules.Where(r => r.IsActive).OrderBy(r => r.Priority).ToList();
        var senderQueries = new List<string>();
        var subjectQueries = new List<string>();
        var keywordQueries = new List<string>();
        var labelQueries = new List<string>();

        foreach (var rule in activeRules)
        {
            // Gmail query syntax for sender filtering
            // Convert simple patterns to Gmail format (exact match or wildcards)
            if (!string.IsNullOrWhiteSpace(rule.SenderPattern))
            {
                // If it's a simple email or domain pattern, use Gmail's from: operator
                var pattern = rule.SenderPattern.Replace(".*", "*").Replace("\\.", ".");
                if (pattern.Contains("@"))
                {
                    senderQueries.Add($"from:{pattern}");
                }
            }

            // Gmail query syntax for subject filtering
            if (!string.IsNullOrWhiteSpace(rule.SubjectPattern))
            {
                // Use subject: operator for simple text patterns
                // Remove regex special chars for Gmail compatibility
                var pattern = System.Text.RegularExpressions.Regex.Replace(
                    rule.SubjectPattern,
                    @"[.*+?^${}()|[\]\\]",
                    ""
                );
                if (!string.IsNullOrWhiteSpace(pattern))
                {
                    subjectQueries.Add($"subject:{pattern}");
                }
            }

            // Gmail query syntax for body/keyword filtering
            if (!string.IsNullOrWhiteSpace(rule.KeywordPattern))
            {
                // Use simple text search for keywords
                var pattern = System.Text.RegularExpressions.Regex.Replace(
                    rule.KeywordPattern,
                    @"[.*+?^${}()|[\]\\]",
                    ""
                );
                if (!string.IsNullOrWhiteSpace(pattern))
                {
                    keywordQueries.Add(pattern);
                }
            }

            // Gmail query syntax for label filtering
            if (!string.IsNullOrWhiteSpace(rule.LabelPattern))
            {
                // Gmail labels use label: or has: operators
                // Examples: label:important, label:finance, has:star
                // Support multiple labels separated by | (pipe) for OR logic
                var labels = rule.LabelPattern.Split(
                    new[] { '|', ',' },
                    StringSplitOptions.RemoveEmptyEntries
                );
                foreach (var label in labels)
                {
                    var cleanLabel = label.Trim().ToLower();
                    // Remove regex special characters
                    cleanLabel = System.Text.RegularExpressions.Regex.Replace(
                        cleanLabel,
                        @"[.*+?^${}()|[\]\\]",
                        ""
                    );

                    if (!string.IsNullOrWhiteSpace(cleanLabel))
                    {
                        labelQueries.Add($"label:{cleanLabel}");
                    }
                }
            }
        }

        // Combine queries with OR operators
        var queryParts = new List<string>();
        if (senderQueries.Any())
            queryParts.Add(string.Join(" OR ", senderQueries));
        if (subjectQueries.Any())
            queryParts.Add(string.Join(" OR ", subjectQueries));
        if (keywordQueries.Any())
            queryParts.Add(string.Join(" OR ", keywordQueries));
        if (labelQueries.Any())
            queryParts.Add(string.Join(" OR ", labelQueries));

        // If we have multiple query parts, combine them with OR
        // This means: match if ANY rule matches
        return queryParts.Any() ? string.Join(" OR ", queryParts) : string.Empty;
    }

    /// <summary>
    /// Applies rule-based filtering to fetched emails.
    /// This is a secondary filter for more complex regex patterns that can't be done at Gmail API level.
    /// </summary>
    private List<EmailMessage> ApplyRuleFiltering(List<EmailMessage> emails, List<Rule>? rules)
    {
        if (rules == null || !rules.Any(r => r.IsActive))
            return emails;

        var activeRules = rules.Where(r => r.IsActive).OrderBy(r => r.Priority).ToList();

        // Filter emails: keep only those that match at least one rule
        return emails.Where(email => activeRules.Any(rule => rule.Matches(email))).ToList();
    }
}
