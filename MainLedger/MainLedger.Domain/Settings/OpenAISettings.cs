namespace MainLedger.Domain.Settings;

/// <summary>
/// Configuration settings for OpenAI API integration.
/// </summary>
public class OpenAISettings
{
    public const string SectionName = "OpenAI";

    /// <summary>
    /// OpenAI API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Model to use for classification (default: gpt-3.5-turbo).
    /// </summary>
    public string Model { get; set; } = "gpt-3.5-turbo";

    /// <summary>
    /// Maximum tokens for API response.
    /// </summary>
    public int MaxTokens { get; set; } = 500;

    /// <summary>
    /// Temperature for response randomness (0.0 = deterministic, 1.0 = creative).
    /// </summary>
    public double Temperature { get; set; } = 0.1;

    /// <summary>
    /// Maximum length of email body to send (for cost control).
    /// </summary>
    public int MaxBodyLength { get; set; } = 2000;
}
