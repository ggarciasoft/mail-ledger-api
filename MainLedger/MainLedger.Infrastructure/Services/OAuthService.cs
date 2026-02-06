using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MainLedger.Application.Common.Interfaces;
using MainLedger.Domain.Settings;
using Microsoft.Extensions.Options;

namespace MainLedger.Infrastructure.Services;

public class OAuthService : IOAuthService
{
    private readonly HttpClient _httpClient;
    private readonly GoogleOAuthSettings _googleSettings;
    private readonly MicrosoftOAuthSettings _microsoftSettings;

    public OAuthService(
        HttpClient httpClient,
        IOptions<GoogleOAuthSettings> googleSettings,
        IOptions<MicrosoftOAuthSettings> microsoftSettings
    )
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _googleSettings = googleSettings?.Value ?? throw new ArgumentNullException(nameof(googleSettings));
        _microsoftSettings = microsoftSettings?.Value ?? throw new ArgumentNullException(nameof(microsoftSettings));
    }

    public Task<string> GetAuthorizationUrlAsync(string provider, string state)
    {
        return provider.ToLowerInvariant() switch
        {
            "google" => Task.FromResult(BuildGoogleAuthUrl(state)),
            "microsoft" => Task.FromResult(BuildMicrosoftAuthUrl(state)),
            _ => throw new ArgumentException($"Unsupported OAuth provider: {provider}", nameof(provider))
        };
    }

    public async Task<OAuthUserInfo> ExchangeCodeForUserInfoAsync(
        string provider,
        string code,
        CancellationToken cancellationToken = default
    )
    {
        return provider.ToLowerInvariant() switch
        {
            "google" => await ExchangeGoogleCodeAsync(code, cancellationToken),
            "microsoft" => await ExchangeMicrosoftCodeAsync(code, cancellationToken),
            _ => throw new ArgumentException($"Unsupported OAuth provider: {provider}", nameof(provider))
        };
    }

    private string BuildGoogleAuthUrl(string state)
    {
        var queryParams = new Dictionary<string, string>
        {
            ["client_id"] = _googleSettings.ClientId,
            ["redirect_uri"] = _googleSettings.RedirectUri,
            ["response_type"] = "code",
            ["scope"] = _googleSettings.Scope,
            ["state"] = state,
            ["access_type"] = "offline",
            ["prompt"] = "consent"
        };

        var query = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
        return $"{_googleSettings.AuthorizationEndpoint}?{query}";
    }

    private string BuildMicrosoftAuthUrl(string state)
    {
        var authEndpoint = _microsoftSettings.AuthorizationEndpoint.Replace("{tenant}", _microsoftSettings.TenantId);
        var queryParams = new Dictionary<string, string>
        {
            ["client_id"] = _microsoftSettings.ClientId,
            ["redirect_uri"] = _microsoftSettings.RedirectUri,
            ["response_type"] = "code",
            ["scope"] = _microsoftSettings.Scope,
            ["state"] = state,
            ["response_mode"] = "query"
        };

        var query = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
        return $"{authEndpoint}?{query}";
    }

    private async Task<OAuthUserInfo> ExchangeGoogleCodeAsync(string code, CancellationToken cancellationToken)
    {
        // Exchange code for access token
        var tokenRequest = new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = _googleSettings.ClientId,
            ["client_secret"] = _googleSettings.ClientSecret,
            ["redirect_uri"] = _googleSettings.RedirectUri,
            ["grant_type"] = "authorization_code"
        };

        var tokenResponse = await _httpClient.PostAsync(
            _googleSettings.TokenEndpoint,
            new FormUrlEncodedContent(tokenRequest),
            cancellationToken
        );

        tokenResponse.EnsureSuccessStatusCode();
        var tokenJson = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
        var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenJson);
        var accessToken = tokenData.GetProperty("access_token").GetString()!;

        // Fetch user info
        var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, _googleSettings.UserInfoEndpoint);
        userInfoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var userInfoResponse = await _httpClient.SendAsync(userInfoRequest, cancellationToken);
        userInfoResponse.EnsureSuccessStatusCode();
        var userInfoJson = await userInfoResponse.Content.ReadAsStringAsync(cancellationToken);
        var userInfo = JsonSerializer.Deserialize<JsonElement>(userInfoJson);

        return new OAuthUserInfo
        {
            ProviderId = userInfo.GetProperty("id").GetString()!,
            Email = userInfo.GetProperty("email").GetString()!,
            FirstName = userInfo.TryGetProperty("given_name", out var givenName) ? givenName.GetString()! : "",
            LastName = userInfo.TryGetProperty("family_name", out var familyName) ? familyName.GetString()! : "",
            Provider = "Google"
        };
    }

    private async Task<OAuthUserInfo> ExchangeMicrosoftCodeAsync(string code, CancellationToken cancellationToken)
    {
        // Exchange code for access token
        var tokenEndpoint = _microsoftSettings.TokenEndpoint.Replace("{tenant}", _microsoftSettings.TenantId);
        var tokenRequest = new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = _microsoftSettings.ClientId,
            ["client_secret"] = _microsoftSettings.ClientSecret,
            ["redirect_uri"] = _microsoftSettings.RedirectUri,
            ["grant_type"] = "authorization_code"
        };

        var tokenResponse = await _httpClient.PostAsync(
            tokenEndpoint,
            new FormUrlEncodedContent(tokenRequest),
            cancellationToken
        );

        if (!tokenResponse.IsSuccessStatusCode)
        {
            var errorContent = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Microsoft token exchange failed with status {tokenResponse.StatusCode}. " +
                $"Response: {errorContent}"
            );
        }

        var tokenJson = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
        var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenJson);
        var accessToken = tokenData.GetProperty("access_token").GetString()!;

        // Fetch user info
        var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, _microsoftSettings.UserInfoEndpoint);
        userInfoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var userInfoResponse = await _httpClient.SendAsync(userInfoRequest, cancellationToken);
        
        if (!userInfoResponse.IsSuccessStatusCode)
        {
            var errorContent = await userInfoResponse.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Microsoft user info request failed with status {userInfoResponse.StatusCode}. " +
                $"Response: {errorContent}"
            );
        }
        var userInfoJson = await userInfoResponse.Content.ReadAsStringAsync(cancellationToken);
        var userInfo = JsonSerializer.Deserialize<JsonElement>(userInfoJson);

        var displayName = userInfo.GetProperty("displayName").GetString() ?? "";
        var nameParts = displayName.Split(' ', 2);

        return new OAuthUserInfo
        {
            ProviderId = userInfo.GetProperty("id").GetString()!,
            Email = userInfo.GetProperty("mail").GetString() ?? userInfo.GetProperty("userPrincipalName").GetString()!,
            FirstName = nameParts.Length > 0 ? nameParts[0] : "",
            LastName = nameParts.Length > 1 ? nameParts[1] : "",
            Provider = "Microsoft"
        };
    }
}
