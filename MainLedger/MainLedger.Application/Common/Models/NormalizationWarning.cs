namespace MainLedger.Application.Common.Models;

/// <summary>
/// Represents a non-critical warning during normalization.
/// </summary>
public class NormalizationWarning
{
    public string Field { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? OriginalValue { get; init; }
    public string? NormalizedValue { get; init; }

    public NormalizationWarning(string field, string message, string? originalValue = null, string? normalizedValue = null)
    {
        Field = field;
        Message = message;
        OriginalValue = originalValue;
        NormalizedValue = normalizedValue;
    }
}
