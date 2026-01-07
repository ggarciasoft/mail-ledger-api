using MainLedger.Application.Common.Models;
using MainLedger.Domain.Entities;

namespace MainLedger.Application.Common.Interfaces;

/// <summary>
/// Service for classifying emails using AI/ML.
/// </summary>
public interface IClassificationService
{
    /// <summary>
    /// Classifies an email to determine if it's financial and what category it belongs to.
    /// </summary>
    /// <param name="email">The email message to classify.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Classification result with confidence score.</returns>
    Task<ClassificationResult> ClassifyEmailAsync(EmailMessage email, CancellationToken cancellationToken = default);
}
