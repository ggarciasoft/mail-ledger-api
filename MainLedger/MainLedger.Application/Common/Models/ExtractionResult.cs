using MainLedger.Domain.ValueObjects;

namespace MainLedger.Application.Common.Models;

/// <summary>
/// Result of financial data extraction from an email.
/// </summary>
public class ExtractionResult
{
    // Core transaction data
    public decimal? Amount { get; init; }
    public string? Currency { get; init; }
    public DateTime? TransactionDate { get; init; }
    public string? Merchant { get; init; }
    
    // Account information
    public string? SourceAccount { get; init; }
    public string? TargetAccount { get; init; }
    public string? SourceBank { get; init; }
    public string? TargetBank { get; init; }
    
    // Additional financial details
    public decimal? Fees { get; init; }
    public decimal? Tax { get; init; }
    public string? ReferenceId { get; init; }
    
    // Per-field confidence scores (0.0 to 1.0)
    public double AmountConfidence { get; init; }
    public double DateConfidence { get; init; }
    public double MerchantConfidence { get; init; }
    
    // Overall extraction quality
    public string Reasoning { get; init; } = string.Empty;
    public bool HasAmbiguities { get; init; }
}
