using MainLedger.Application.Common.Models;
using MainLedger.Domain.Entities;

namespace MainLedger.Application.Common.Interfaces;

/// <summary>
/// Service for extracting structured financial data from emails using AI/ML.
/// </summary>
public interface IExtractionService
{
    /// <summary>
    /// Extracts financial data from a classified email.
    /// </summary>
    /// <param name="email">The email message to extract data from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Extraction result with structured financial data and confidence scores.</returns>
    Task<ExtractionResult> ExtractFinancialDataAsync(EmailMessage email, CancellationToken cancellationToken = default);
}
