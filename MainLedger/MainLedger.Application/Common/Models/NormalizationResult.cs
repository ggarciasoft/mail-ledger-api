using MainLedger.Domain.ValueObjects;

namespace MainLedger.Application.Common.Models;

/// <summary>
/// Result of normalization process containing validated and standardized data.
/// </summary>
public class NormalizationResult
{
    // Normalized fields (ready for database)
    public decimal? NormalizedAmount { get; init; }
    public string? NormalizedCurrency { get; init; }
    public DateTime? NormalizedDate { get; init; }
    public string? NormalizedMerchant { get; init; }
    public string? NormalizedSourceAccount { get; init; }
    public string? NormalizedTargetAccount { get; init; }
    public string? NormalizedSourceBank { get; init; }
    public string? NormalizedTargetBank { get; init; }
    public decimal? NormalizedFees { get; init; }
    public decimal? NormalizedTax { get; init; }
    public string? ReferenceId { get; init; }
    
    // Deduplication
    public string DeduplicationHash { get; init; } = string.Empty;
    
    // Validation results
    public List<NormalizationError> Errors { get; init; } = new();
    public List<NormalizationWarning> Warnings { get; init; } = new();
    
    // Status
    public bool IsValid => Errors.Count == 0;
    public bool HasWarnings => Warnings.Count > 0;
    
    // Confidence scores (passed through from extraction)
    public double AmountConfidence { get; init; }
    public double DateConfidence { get; init; }
    public double MerchantConfidence { get; init; }
}
