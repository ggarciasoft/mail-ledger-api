using MainLedger.Domain.Common;

namespace MainLedger.Domain.ValueObjects;

/// <summary>
/// Represents a bank provider.
/// Allows dynamic bank additions without code changes.
/// </summary>
public sealed class BankProvider : ValueObject
{
    public string Name { get; }
    public string? Code { get; }

    private BankProvider(string name, string? code = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Bank name cannot be empty.", nameof(name));
        }

        Name = name;
        Code = code;
    }

    /// <summary>
    /// Creates a custom bank provider.
    /// </summary>
    public static BankProvider Create(string name, string? code = null)
    {
        return new BankProvider(name, code);
    }

    // Common Dominican Republic banks as static properties
    public static BankProvider BHD => new("Banco BHD León", "BHD");
    public static BankProvider Popular => new("Banco Popular Dominicano", "POPULAR");
    public static BankProvider BanReservas => new("Banco de Reservas", "BANRESERVAS");
    public static BankProvider ScotiaBank => new("Scotiabank", "SCOTIA");
    public static BankProvider BDI => new("Banco BDI", "BDI");
    public static BankProvider SantaCruz => new("Banco Santa Cruz", "SANTACRUZ");
    public static BankProvider Lopez => new("Banco López de Haro", "LOPEZ");
    public static BankProvider Promerica => new("Banco Promerica", "PROMERICA");
    public static BankProvider Ademi => new("Banco Ademi", "ADEMI");
    public static BankProvider Unknown => new("Unknown Bank", "UNKNOWN");

    public override string ToString()
    {
        return Code != null ? $"{Name} ({Code})" : Name;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Name;
        yield return Code;
    }
}
