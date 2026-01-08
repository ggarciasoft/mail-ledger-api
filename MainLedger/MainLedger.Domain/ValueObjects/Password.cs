using MainLedger.Domain.Common;
using System.Text.RegularExpressions;

namespace MainLedger.Domain.ValueObjects;

/// <summary>
/// Represents a validated password with strength requirements.
/// This is for validation only - passwords should be hashed before storage.
/// </summary>
public sealed class Password : ValueObject
{
    private static readonly Regex UppercasePattern = new(@"[A-Z]", RegexOptions.Compiled);
    private static readonly Regex LowercasePattern = new(@"[a-z]", RegexOptions.Compiled);
    private static readonly Regex DigitPattern = new(@"[0-9]", RegexOptions.Compiled);
    private static readonly Regex SpecialCharPattern = new(@"[!@#$%^&*(),.?""':{}|<>]", RegexOptions.Compiled);

    public const int MinimumLength = 8;
    public const int MaximumLength = 128;

    public string Value { get; }

    private Password(string value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Creates a new Password instance with validation.
    /// </summary>
    public static Password Create(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty.", nameof(password));

        if (password.Length < MinimumLength)
            throw new ArgumentException($"Password must be at least {MinimumLength} characters long.", nameof(password));

        if (password.Length > MaximumLength)
            throw new ArgumentException($"Password cannot exceed {MaximumLength} characters.", nameof(password));

        if (!UppercasePattern.IsMatch(password))
            throw new ArgumentException("Password must contain at least one uppercase letter.", nameof(password));

        if (!LowercasePattern.IsMatch(password))
            throw new ArgumentException("Password must contain at least one lowercase letter.", nameof(password));

        if (!DigitPattern.IsMatch(password))
            throw new ArgumentException("Password must contain at least one digit.", nameof(password));

        // Special characters are recommended but not required
        // You can uncomment this if you want to enforce special characters
        // if (!SpecialCharPattern.IsMatch(password))
        //     throw new ArgumentException("Password must contain at least one special character.", nameof(password));

        return new Password(password);
    }

    /// <summary>
    /// Validates a password without creating an instance.
    /// Returns true if valid, false otherwise.
    /// </summary>
    public static bool IsValid(string password, out string? errorMessage)
    {
        try
        {
            Create(password);
            errorMessage = null;
            return true;
        }
        catch (ArgumentException ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Calculates password strength (0-100).
    /// </summary>
    public static int CalculateStrength(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return 0;

        int strength = 0;

        // Length score (max 40 points)
        strength += Math.Min(password.Length * 2, 40);

        // Character variety (max 60 points)
        if (UppercasePattern.IsMatch(password)) strength += 15;
        if (LowercasePattern.IsMatch(password)) strength += 15;
        if (DigitPattern.IsMatch(password)) strength += 15;
        if (SpecialCharPattern.IsMatch(password)) strength += 15;

        return Math.Min(strength, 100);
    }

    public override string ToString()
    {
        return "********"; // Never expose the actual password
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
