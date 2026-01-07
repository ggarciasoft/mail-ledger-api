using MainLedger.Domain.Enums;
using MainLedger.Domain.ValueObjects;

namespace MainLedger.Application.Common.Models;

/// <summary>
/// Result of email classification.
/// </summary>
public class ClassificationResult
{
    /// <summary>
    /// Whether the email is financial or not.
    /// </summary>
    public bool IsFinancial { get; init; }

    /// <summary>
    /// Category of financial email (null if not financial).
    /// </summary>
    public EmailCategory? Category { get; init; }

    /// <summary>
    /// Confidence score (0.0 to 1.0).
    /// </summary>
    public Confidence Confidence { get; init; } = null!;

    /// <summary>
    /// Reasoning or explanation for the classification.
    /// </summary>
    public string Reasoning { get; init; } = string.Empty;
}
