using MainLedger.Domain.Common;
using System.Security.Cryptography;
using System.Text;

namespace MainLedger.Domain.ValueObjects;

/// <summary>
/// Represents an API key value with generation and masking capabilities.
/// Format: mlk_{environment}_{random}
/// Example: mlk_live_a1b2c3d4e5f6g7h8i9j0
/// </summary>
public sealed class ApiKeyValue : ValueObject
{
    private const string Prefix = "mlk_";
    private const int RandomPartLength = 32;

    public string Value { get; }
    public string Environment { get; }

    private ApiKeyValue(string value, string environment)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
        Environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    /// <summary>
    /// Generates a new cryptographically secure API key.
    /// </summary>
    public static ApiKeyValue Generate(string environment = "live")
    {
        if (string.IsNullOrWhiteSpace(environment))
            throw new ArgumentException("Environment cannot be empty.", nameof(environment));

        var randomPart = GenerateRandomString(RandomPartLength);
        var value = $"{Prefix}{environment}_{randomPart}";

        return new ApiKeyValue(value, environment);
    }

    /// <summary>
    /// Creates an ApiKeyValue from an existing key string.
    /// </summary>
    public static ApiKeyValue FromString(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("API key cannot be empty.", nameof(apiKey));

        if (!apiKey.StartsWith(Prefix))
            throw new ArgumentException($"API key must start with '{Prefix}'.", nameof(apiKey));

        var parts = apiKey.Split('_');
        if (parts.Length < 3)
            throw new ArgumentException("Invalid API key format.", nameof(apiKey));

        var environment = parts[1];
        return new ApiKeyValue(apiKey, environment);
    }

    /// <summary>
    /// Masks the API key for display purposes.
    /// Example: mlk_live_a1b2c3d4e5f6g7h8i9j0 -> mlk_live_****j0
    /// </summary>
    public string Mask()
    {
        if (Value.Length <= 10)
            return "****";

        var parts = Value.Split('_');
        if (parts.Length < 3)
            return "****";

        var randomPart = parts[2];
        var lastFour = randomPart.Length >= 4 ? randomPart.Substring(randomPart.Length - 4) : randomPart;

        return $"{Prefix}{Environment}_****{lastFour}";
    }

    /// <summary>
    /// Generates a cryptographically secure random string.
    /// </summary>
    private static string GenerateRandomString(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        var bytes = new byte[length];
        
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }

        var result = new StringBuilder(length);
        foreach (var b in bytes)
        {
            result.Append(chars[b % chars.Length]);
        }

        return result.ToString();
    }

    public override string ToString()
    {
        return Value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
