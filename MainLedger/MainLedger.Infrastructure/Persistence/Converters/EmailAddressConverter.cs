using MainLedger.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MainLedger.Infrastructure.Persistence.Converters;

/// <summary>
/// Converts EmailAddress value object to/from string for database storage.
/// </summary>
public class EmailAddressConverter : ValueConverter<EmailAddress, string>
{
    public EmailAddressConverter()
        : base(
            email => email.Value,
            value => EmailAddress.Create(value))
    {
    }
}
