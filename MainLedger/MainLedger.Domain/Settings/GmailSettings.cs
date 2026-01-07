namespace MainLedger.Domain.Settings;

/// <summary>
/// Configuration settings for Gmail API integration.
/// </summary>
public class GmailSettings
{
    public const string SectionName = "Gmail";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
}
