using MainLedger.Domain.Common;
using System.Text.RegularExpressions;

namespace MainLedger.Domain.ValueObjects;

/// <summary>
/// Represents a masked account number (e.g., "***1234").
/// Immutable value object for account identification.
/// </summary>
public sealed class AccountNumber : ValueObject
{
    private static readonly Regex MaskedAccountPattern = new(@"^\*+\d{4}$", RegexOptions.Compiled);

    public string Value { get; }

    private AccountNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Account number cannot be empty.", nameof(value));
        }

        if (!MaskedAccountPattern.IsMatch(value))
        {
            throw new ArgumentException(
                "Account number must be in masked format (e.g., '***1234').", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// Creates a new AccountNumber instance from a masked account string.
    /// </summary>
    public static AccountNumber Create(string maskedAccount)
    {
        return new AccountNumber(maskedAccount);
    }

    /// <summary>
    /// Creates a masked account number from the last 4 digits.
    /// </summary>
    public static AccountNumber FromLastFourDigits(string lastFour)
    {
        if (string.IsNullOrWhiteSpace(lastFour) || lastFour.Length != 4 || !lastFour.All(char.IsDigit))
        {
            throw new ArgumentException("Must provide exactly 4 digits.", nameof(lastFour));
        }

        return new AccountNumber($"***{lastFour}");
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
