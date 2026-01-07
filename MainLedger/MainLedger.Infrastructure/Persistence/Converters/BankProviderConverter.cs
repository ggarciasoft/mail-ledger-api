using MainLedger.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace MainLedger.Infrastructure.Persistence.Converters;

/// <summary>
/// Converts BankProvider value object to/from JSON string for database storage.
/// Stores both Name and Code as JSON.
/// </summary>
public class BankProviderConverter : ValueConverter<BankProvider, string>
{
    public BankProviderConverter()
        : base(
            bankProvider => SerializeBankProvider(bankProvider),
            json => DeserializeBankProvider(json))
    {
    }

    private static string SerializeBankProvider(BankProvider bankProvider)
    {
        var data = new { Name = bankProvider.Name, Code = bankProvider.Code };
        return JsonSerializer.Serialize(data);
    }

    private static BankProvider DeserializeBankProvider(string json)
    {
        var data = JsonSerializer.Deserialize<BankProviderData>(json);
        return BankProvider.Create(data!.Name, data.Code);
    }

    private class BankProviderData
    {
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
    }
}
