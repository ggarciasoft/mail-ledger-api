namespace MainLedger.Domain.Settings;

/// <summary>
/// Configuration settings for Google OAuth SSO authentication.
/// </summary>
public class GoogleOAuthSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string AuthorizationEndpoint { get; set; } = "https://accounts.google.com/o/oauth2/v2/auth";
    public string TokenEndpoint { get; set; } = "https://oauth2.googleapis.com/token";
    public string UserInfoEndpoint { get; set; } = "https://www.googleapis.com/oauth2/v2/userinfo";
    public string Scope { get; set; } = "openid email profile";
}

/// <summary>
/// Configuration settings for Microsoft OAuth SSO authentication.
/// </summary>
public class MicrosoftOAuthSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string TenantId { get; set; } = "common";
    public string RedirectUri { get; set; } = string.Empty;
    public string AuthorizationEndpoint { get; set; } = "https://login.microsoftonline.com/{tenant}/oauth2/v2.0/authorize";
    public string TokenEndpoint { get; set; } = "https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token";
    public string UserInfoEndpoint { get; set; } = "https://graph.microsoft.com/v1.0/me";
    public string Scope { get; set; } = "openid email profile User.Read";
}

/// <summary>
/// User information returned from OAuth providers.
/// </summary>
public class OAuthUserInfo
{
    public string ProviderId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
}
