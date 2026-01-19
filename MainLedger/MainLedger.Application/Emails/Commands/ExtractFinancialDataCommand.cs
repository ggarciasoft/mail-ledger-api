using MainLedger.Application.Common.Interfaces;
using MainLedger.Application.Common.Models;
using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using MainLedger.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.Emails.Commands;

/// <summary>
/// Command to extract financial data from a classified email.
/// </summary>
public record ExtractFinancialDataCommand(Guid EmailId) : IRequest<ExtractionCandidate>;

public class ExtractFinancialDataCommandHandler
    : IRequestHandler<ExtractFinancialDataCommand, ExtractionCandidate>
{
    private readonly IEmailMessageRepository _emailRepository;
    private readonly IExtractionService _extractionService;
    private readonly INormalizationService _normalizationService;
    private readonly IExtractionVersionRepository _versionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ExtractFinancialDataCommandHandler> _logger;

    public ExtractFinancialDataCommandHandler(
        IEmailMessageRepository emailRepository,
        IExtractionService extractionService,
        INormalizationService normalizationService,
        IExtractionVersionRepository versionRepository,
        IUnitOfWork unitOfWork,
        ILogger<ExtractFinancialDataCommandHandler> logger
    )
    {
        _emailRepository = emailRepository;
        _extractionService = extractionService;
        _normalizationService = normalizationService;
        _versionRepository = versionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ExtractionCandidate> Handle(
        ExtractFinancialDataCommand request,
        CancellationToken cancellationToken
    )
    {
        var email = await _emailRepository.GetByIdAsync(request.EmailId, cancellationToken);

        if (email == null)
        {
            throw new KeyNotFoundException($"Email not found: {request.EmailId}");
        }

        if (email.IsFinancial != true)
        {
            throw new InvalidOperationException(
                $"Email {request.EmailId} is not classified as financial"
            );
        }

        _logger.LogInformation("Extracting financial data from email {EmailId}", request.EmailId);

        // Get active extraction version
        var version = await _versionRepository.GetActiveAsync(cancellationToken);
        if (version == null)
        {
            throw new InvalidOperationException("No active extraction version found");
        }

        // Call extraction service
        var extractionResult = await _extractionService.ExtractFinancialDataAsync(
            email,
            cancellationToken
        );

        // Normalize extracted data
        var normalizationResult = await _normalizationService.NormalizeExtractionAsync(
            extractionResult,
            email,
            cancellationToken
        );

        // Check for validation errors
        if (!normalizationResult.IsValid)
        {
            var errorMessages = string.Join(
                ", ",
                normalizationResult.Errors.Select(e => $"{e.Field}: {e.Message}")
            );
            _logger.LogWarning(
                "Normalization failed for email {EmailId}: {Errors}",
                request.EmailId,
                errorMessages
            );

            throw new InvalidOperationException(
                $"Extraction normalization failed: {errorMessages}"
            );
        }

        // Log warnings if any
        if (normalizationResult.HasWarnings)
        {
            var warningMessages = string.Join(
                ", ",
                normalizationResult.Warnings.Select(w => $"{w.Field}: {w.Message}")
            );
            _logger.LogWarning(
                "Normalization warnings for email {EmailId}: {Warnings}",
                request.EmailId,
                warningMessages
            );
        }

        // Create extraction candidate with normalized data
        var candidate = ExtractionCandidate.Create(email.Id, version.Id);

        // Set transaction data using normalized values
        Money? amount = null;
        if (
            normalizationResult.NormalizedAmount.HasValue
            && !string.IsNullOrWhiteSpace(normalizationResult.NormalizedCurrency)
        )
        {
            if (
                Enum.TryParse<Currency>(
                    normalizationResult.NormalizedCurrency,
                    true,
                    out var currency
                )
            )
            {
                amount = Money.Create(normalizationResult.NormalizedAmount.Value, currency);
            }
        }

        candidate.SetTransactionData(
            amount,
            normalizationResult.NormalizedDate,
            normalizationResult.NormalizedMerchant,
            null, // merchantOriginal - not available in this legacy path
            normalizationResult.AmountConfidence > 0
                ? Confidence.Create(normalizationResult.AmountConfidence)
                : null,
            normalizationResult.DateConfidence > 0
                ? Confidence.Create(normalizationResult.DateConfidence)
                : null,
            normalizationResult.MerchantConfidence > 0
                ? Confidence.Create(normalizationResult.MerchantConfidence)
                : null
        );
        // Set account info using normalized values
        AccountNumber? sourceAccount = null;
        AccountNumber? targetAccount = null;

        if (!string.IsNullOrWhiteSpace(normalizationResult.NormalizedSourceAccount))
        {
            sourceAccount = AccountNumber.Create(normalizationResult.NormalizedSourceAccount);
        }

        if (!string.IsNullOrWhiteSpace(normalizationResult.NormalizedTargetAccount))
        {
            targetAccount = AccountNumber.Create(normalizationResult.NormalizedTargetAccount);
        }

        BankProvider? sourceBank = null;
        BankProvider? targetBank = null;

        if (!string.IsNullOrWhiteSpace(normalizationResult.NormalizedSourceBank))
        {
            sourceBank = BankProvider.Create(
                normalizationResult.NormalizedSourceBank,
                normalizationResult.NormalizedSourceBank
            );
        }

        if (!string.IsNullOrWhiteSpace(normalizationResult.NormalizedTargetBank))
        {
            targetBank = BankProvider.Create(
                normalizationResult.NormalizedTargetBank,
                normalizationResult.NormalizedTargetBank
            );
        }

        candidate.SetAccountInfo(sourceAccount, targetAccount, sourceBank, targetBank);

        // Set additional details using normalized values
        Money? fees = null;
        Money? tax = null;

        if (normalizationResult.NormalizedFees.HasValue && amount != null)
        {
            fees = Money.Create(normalizationResult.NormalizedFees.Value, amount.Currency);
        }

        if (normalizationResult.NormalizedTax.HasValue && amount != null)
        {
            tax = Money.Create(normalizationResult.NormalizedTax.Value, amount.Currency);
        }

        candidate.SetAdditionalDetails(fees, tax, normalizationResult.ReferenceId);

        // Save candidate (repository will be added later)
        // await _candidateRepository.AddAsync(candidate, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Financial data extracted and normalized from email {EmailId}: Amount={Amount}, Merchant={Merchant}, Hash={Hash}",
            request.EmailId,
            amount,
            normalizationResult.NormalizedMerchant,
            normalizationResult.DeduplicationHash
        );

        return candidate;
    }
}
