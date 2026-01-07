using MainLedger.Domain.Common;

namespace MainLedger.Domain.ValueObjects;

/// <summary>
/// Represents an AI confidence score between 0.0 and 1.0.
/// Used to indicate the reliability of extracted financial data.
/// </summary>
public sealed class Confidence : ValueObject
{
    public double Value { get; }

    private Confidence(double value)
    {
        if (value < 0.0 || value > 1.0)
        {
            throw new ArgumentException("Confidence must be between 0.0 and 1.0.", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// Creates a new Confidence instance.
    /// </summary>
    public static Confidence Create(double value)
    {
        return new Confidence(value);
    }

    /// <summary>
    /// Returns true if confidence is high (>= 0.8).
    /// </summary>
    public bool IsHigh() => Value >= 0.8;

    /// <summary>
    /// Returns true if confidence is medium (>= 0.5 and < 0.8).
    /// </summary>
    public bool IsMedium() => Value >= 0.5 && Value < 0.8;

    /// <summary>
    /// Returns true if confidence is low (< 0.5).
    /// </summary>
    public bool IsLow() => Value < 0.5;

    public override string ToString()
    {
        return $"{Value:P0}";
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
