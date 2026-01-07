using MainLedger.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MainLedger.Infrastructure.Persistence.Converters;

/// <summary>
/// Converts Confidence value object to/from double for database storage.
/// </summary>
public class ConfidenceConverter : ValueConverter<Confidence, double>
{
    public ConfidenceConverter()
        : base(
            confidence => confidence.Value,
            value => Confidence.Create(value))
    {
    }
}
