using MainLedger.Application.Common.Models;
using MainLedger.Domain.Entities;

namespace MainLedger.Application.Common.Interfaces;

/// <summary>
/// Service for normalizing and validating extracted financial data.
/// </summary>
public interface INormalizationService
{
    /// <summary>
    /// Normalizes extraction result into standardized, validated format.
    /// </summary>
    /// <param name="extraction">Raw extraction result from AI</param>
    /// <param name="email">Source email message for context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Normalized result with validation errors/warnings</returns>
    Task<NormalizationResult> NormalizeExtractionAsync(
        ExtractionResult extraction,
        EmailMessage email,
        CancellationToken cancellationToken = default);
}
