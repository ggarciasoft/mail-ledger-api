using MainLedger.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MainLedger.Infrastructure.Persistence.Converters;

/// <summary>
/// Converts AccountNumber value object to/from string for database storage.
/// </summary>
public class AccountNumberConverter : ValueConverter<AccountNumber, string>
{
    public AccountNumberConverter()
        : base(
            accountNumber => accountNumber.Value,
            value => AccountNumber.Create(value))
    {
    }
}
