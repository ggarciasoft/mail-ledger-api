namespace MainLedger.Application.Common.Models;

/// <summary>
/// Represents a validation error during normalization.
/// </summary>
public class NormalizationError
{
    public string Field { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? OriginalValue { get; init; }

    public NormalizationError(string field, string message, string? originalValue = null)
    {
        Field = field;
        Message = message;
        OriginalValue = originalValue;
    }
}
