using System.Security.Claims;
using System.Text.Encodings.Web;
using MainLedger.Domain.Repositories;
using MainLedger.Domain.Services;
using MainLedger.Domain.ValueObjects;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MainLedger.Infrastructure.Security;

/// <summary>
/// Authentication handler for API key authentication.
/// Validates API keys from the Authorization header (Bearer scheme).
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IApiKeyRepository _apiKeyRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<ApiKeyAuthenticationHandler> _logger;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        IApiKeyRepository apiKeyRepository,
        IPasswordHasher passwordHasher
    )
        : base(options, logger, encoder, clock)
    {
        _apiKeyRepository = apiKeyRepository;
        _passwordHasher = passwordHasher;
        _logger = logger.CreateLogger<ApiKeyAuthenticationHandler>();
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check if Authorization header exists
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return AuthenticateResult.NoResult();
        }

        string? authorizationHeader = Request.Headers["Authorization"];
        if (string.IsNullOrWhiteSpace(authorizationHeader))
        {
            return AuthenticateResult.NoResult();
        }

        // Check if it's a Bearer token
        if (!authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        // Extract the API key
        var apiKeyValue = authorizationHeader.Substring("Bearer ".Length).Trim();

        // Validate API key format
        if (!apiKeyValue.StartsWith("mlk_"))
        {
            // Not an API key format, let JWT handler try
            return AuthenticateResult.NoResult();
        }

        try
        {
            // Parse API key
            var apiKey = ApiKeyValue.FromString(apiKeyValue);
            var maskedKey = apiKey.Mask();

            // Get all active API keys and verify hash
            // Note: We must iterate because PBKDF2 uses salt, so we can't query by hash directly
            var activeApiKeys = await _apiKeyRepository.GetAllActiveAsync(CancellationToken.None);

            Domain.Entities.ApiKey? matchedApiKey = null;
            foreach (var dbKey in activeApiKeys)
            {
                if (_passwordHasher.VerifyPassword(apiKeyValue, dbKey.KeyHash))
                {
                    matchedApiKey = dbKey;
                    break;
                }
            }

            if (matchedApiKey == null)
            {
                _logger.LogWarning("API key not found or inactive: {MaskedKey}", maskedKey);
                return AuthenticateResult.Fail("Invalid API key");
            }

            // Check if API key is active
            if (!matchedApiKey.IsActive)
            {
                _logger.LogWarning("Inactive API key used: {ApiKeyId}", matchedApiKey.Id);
                return AuthenticateResult.Fail("API key is inactive");
            }

            // Check if API key is expired
            if (matchedApiKey.ExpiresAt.HasValue && matchedApiKey.ExpiresAt.Value < DateTime.UtcNow)
            {
                _logger.LogWarning("Expired API key used: {ApiKeyId}", matchedApiKey.Id);
                return AuthenticateResult.Fail("API key has expired");
            }

            // Update last used timestamp (fire and forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    matchedApiKey.RecordUsage();
                    await _apiKeyRepository.UpdateAsync(matchedApiKey, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update API key last used timestamp");
                }
            });

            // Create claims
            var claims = new List<Claim>
            {
                new Claim("userId", matchedApiKey.UserId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, matchedApiKey.UserId.ToString()),
                new Claim("apiKeyId", matchedApiKey.Id.ToString()),
                new Claim("authType", "ApiKey"),
            };

            // Add scope claims
            foreach (var scope in matchedApiKey.Scopes)
            {
                claims.Add(new Claim("scope", scope));
            }

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            _logger.LogInformation(
                "API key authenticated successfully: {ApiKeyId} for user {UserId}",
                matchedApiKey.Id,
                matchedApiKey.UserId
            );

            return AuthenticateResult.Success(ticket);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid API key format");
            return AuthenticateResult.Fail("Invalid API key format");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating API key");
            return AuthenticateResult.Fail("Authentication error");
        }
    }
}
