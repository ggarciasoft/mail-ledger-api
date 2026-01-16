namespace MainLedger.Domain.Settings;

public class OutlookSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string TenantId { get; set; } = "common";
    public string RedirectUri { get; set; } = string.Empty;
    public List<string> Scopes { get; set; } = new() { "Mail.Read", "User.Read" };
}
