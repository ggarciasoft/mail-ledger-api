using MainLedger.Domain.Common;
using MainLedger.Domain.Enums;

namespace MainLedger.Domain.ValueObjects;

/// <summary>
/// Represents a monetary amount with its currency.
/// Immutable value object ensuring type safety for financial operations.
/// </summary>
public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public Currency Currency { get; }

    private Money(decimal amount, Currency currency)
    {
        if (amount < 0)
        {
            throw new ArgumentException("Amount cannot be negative.", nameof(amount));
        }

        Amount = amount;
        Currency = currency;
    }

    /// <summary>
    /// Creates a new Money instance.
    /// </summary>
    public static Money Create(decimal amount, Currency currency)
    {
        return new Money(amount, currency);
    }

    /// <summary>
    /// Creates a Money instance with zero amount.
    /// </summary>
    public static Money Zero(Currency currency)
    {
        return new Money(0, currency);
    }

    /// <summary>
    /// Adds two Money values. Both must have the same currency.
    /// </summary>
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
        {
            throw new InvalidOperationException(
                $"Cannot add money with different currencies: {Currency} and {other.Currency}");
        }

        return new Money(Amount + other.Amount, Currency);
    }

    /// <summary>
    /// Subtracts two Money values. Both must have the same currency.
    /// </summary>
    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
        {
            throw new InvalidOperationException(
                $"Cannot subtract money with different currencies: {Currency} and {other.Currency}");
        }

        var result = Amount - other.Amount;
        if (result < 0)
        {
            throw new InvalidOperationException("Subtraction would result in negative amount.");
        }

        return new Money(result, Currency);
    }

    /// <summary>
    /// Multiplies the amount by a factor.
    /// </summary>
    public Money Multiply(decimal factor)
    {
        if (factor < 0)
        {
            throw new ArgumentException("Factor cannot be negative.", nameof(factor));
        }

        return new Money(Amount * factor, Currency);
    }

    public override string ToString()
    {
        return $"{Amount:N2} {Currency}";
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
