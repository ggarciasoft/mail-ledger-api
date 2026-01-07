using MainLedger.Domain.Common;
using System.Text.RegularExpressions;

namespace MainLedger.Domain.ValueObjects;

/// <summary>
/// Represents a validated email address.
/// Immutable value object.
/// </summary>
public sealed class EmailAddress : ValueObject
{
    private static readonly Regex EmailPattern = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    private EmailAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Email address cannot be empty.", nameof(value));
        }

        if (!EmailPattern.IsMatch(value))
        {
            throw new ArgumentException("Invalid email address format.", nameof(value));
        }

        Value = value.ToLowerInvariant();
    }

    /// <summary>
    /// Creates a new EmailAddress instance.
    /// </summary>
    public static EmailAddress Create(string email)
    {
        return new EmailAddress(email);
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
