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
/// Command to batch extract financial data from classified emails.
/// Processes up to a specified number of financial emails in parallel.
/// </summary>
public record BatchExtractFinancialDataCommand(Guid UserId, int BatchSize = 20)
    : IRequest<BatchExtractionResult>;

public record BatchExtractionResult
{
    public int EmailsProcessed { get; init; }
    public int EmailsExtracted { get; init; }
    public int EmailsFailed { get; init; }
    public string Status { get; init; } = string.Empty;
}

public class BatchExtractFinancialDataCommandHandler
    : IRequestHandler<BatchExtractFinancialDataCommand, BatchExtractionResult>
{
    private readonly IEmailMessageRepository _emailRepository;
    private readonly IExtractionService _extractionService;
    private readonly INormalizationService _normalizationService;
    private readonly IExtractionVersionRepository _versionRepository;
    private readonly IExtractionCandidateRepository _candidateRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BatchExtractFinancialDataCommandHandler> _logger;

    public BatchExtractFinancialDataCommandHandler(
        IEmailMessageRepository emailRepository,
        IExtractionService extractionService,
        INormalizationService normalizationService,
        IExtractionVersionRepository versionRepository,
        IExtractionCandidateRepository candidateRepository,
        IUnitOfWork unitOfWork,
        ILogger<BatchExtractFinancialDataCommandHandler> logger
    )
    {
        _emailRepository = emailRepository;
        _extractionService = extractionService;
        _normalizationService = normalizationService;
        _versionRepository = versionRepository;
        _candidateRepository = candidateRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<BatchExtractionResult> Handle(
        BatchExtractFinancialDataCommand request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogInformation(
            "Starting batch extraction for user {UserId} with batch size {BatchSize}",
            request.UserId,
            request.BatchSize
        );

        // Get active extraction version
        var version = await _versionRepository.GetActiveAsync(cancellationToken);
        if (version == null)
        {
            throw new InvalidOperationException("No active extraction version found");
        }

        // Fetch classified financial emails for this user
        var classifiedEmails = await _emailRepository.GetClassifiedFinancialEmailsAsync(
            request.UserId,
            request.BatchSize,
            cancellationToken
        );

        if (!classifiedEmails.Any())
        {
            _logger.LogInformation(
                "No classified financial emails found for user {UserId}",
                request.UserId
            );
            return new BatchExtractionResult
            {
                EmailsProcessed = 0,
                EmailsExtracted = 0,
                EmailsFailed = 0,
                Status = "No classified financial emails",
            };
        }

        int extractedCount = 0;
        int failedCount = 0;
        var candidates = new List<ExtractionCandidate>();

        // Process emails in parallel (with controlled concurrency)
        var extractionTasks = classifiedEmails.Select(async email =>
        {
            try
            {
                _logger.LogDebug(
                    "Extracting financial data from email {MessageId}",
                    email.MessageId
                );

                // Extract data
                var extractionResult = await _extractionService.ExtractFinancialDataAsync(
                    email,
                    cancellationToken
                );

                // Normalize data
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
                        "Normalization failed for email {MessageId}: {Errors}",
                        email.MessageId,
                        errorMessages
                    );

                    email.SetProcessingStatus(
                        EmailProcessingStatus.Failed,
                        $"Normalization failed: {errorMessages}"
                    );
                    Interlocked.Increment(ref failedCount);
                    return;
                }

                // Create extraction candidate with normalized data
                var candidate = CreateExtractionCandidate(
                    email.Id,
                    version.Id,
                    normalizationResult
                );

                lock (candidates)
                {
                    candidates.Add(candidate);
                }

                email.SetProcessingStatus(EmailProcessingStatus.Extracted);

                _logger.LogInformation(
                    "Email {MessageId} extracted: Amount={Amount}, Merchant={Merchant}",
                    email.MessageId,
                    normalizationResult.NormalizedAmount,
                    normalizationResult.NormalizedMerchant
                );

                Interlocked.Increment(ref extractedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Extraction failed for email {MessageId}", email.MessageId);
                email.SetProcessingStatus(EmailProcessingStatus.Failed, ex.Message);
                Interlocked.Increment(ref failedCount);
            }
        });

        // Wait for all extractions to complete
        await Task.WhenAll(extractionTasks);

        // Save extraction candidates to database
        foreach (var candidate in candidates)
        {
            await _candidateRepository.AddAsync(candidate, cancellationToken);
        }

        // Save all changes (candidates and email statuses)
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Batch extraction completed: {Processed} processed, {Extracted} extracted, {Failed} failed",
            classifiedEmails.Count,
            extractedCount,
            failedCount
        );

        return new BatchExtractionResult
        {
            EmailsProcessed = classifiedEmails.Count,
            EmailsExtracted = extractedCount,
            EmailsFailed = failedCount,
            Status = "Success",
        };
    }

    private ExtractionCandidate CreateExtractionCandidate(
        Guid emailId,
        Guid versionId,
        NormalizationResult result
    )
    {
        var candidate = ExtractionCandidate.Create(emailId, versionId);

        // Set transaction data
        Money? amount = null;
        if (
            result.NormalizedAmount.HasValue
            && !string.IsNullOrWhiteSpace(result.NormalizedCurrency)
        )
        {
            if (Enum.TryParse<Currency>(result.NormalizedCurrency, true, out var currency))
            {
                amount = Money.Create(result.NormalizedAmount.Value, currency);
            }
        }

        candidate.SetTransactionData(
            amount,
            result.NormalizedDate,
            result.NormalizedMerchant,
            null, // merchantOriginal - not available in this legacy path
            result.AmountConfidence > 0 ? Confidence.Create(result.AmountConfidence) : null,
            result.DateConfidence > 0 ? Confidence.Create(result.DateConfidence) : null,
            result.MerchantConfidence > 0 ? Confidence.Create(result.MerchantConfidence) : null
        );

        // Set account info
        AccountNumber? sourceAccount = null;
        AccountNumber? targetAccount = null;

        if (!string.IsNullOrWhiteSpace(result.NormalizedSourceAccount))
        {
            sourceAccount = AccountNumber.Create(result.NormalizedSourceAccount);
        }

        if (!string.IsNullOrWhiteSpace(result.NormalizedTargetAccount))
        {
            targetAccount = AccountNumber.Create(result.NormalizedTargetAccount);
        }

        BankProvider? sourceBank = null;
        BankProvider? targetBank = null;

        if (!string.IsNullOrWhiteSpace(result.NormalizedSourceBank))
        {
            sourceBank = BankProvider.Create(
                result.NormalizedSourceBank,
                result.NormalizedSourceBank
            );
        }

        if (!string.IsNullOrWhiteSpace(result.NormalizedTargetBank))
        {
            targetBank = BankProvider.Create(
                result.NormalizedTargetBank,
                result.NormalizedTargetBank
            );
        }

        candidate.SetAccountInfo(sourceAccount, targetAccount, sourceBank, targetBank);

        // Set additional details
        Money? fees = null;
        Money? tax = null;

        if (result.NormalizedFees.HasValue && amount != null)
        {
            fees = Money.Create(result.NormalizedFees.Value, amount.Currency);
        }

        if (result.NormalizedTax.HasValue && amount != null)
        {
            tax = Money.Create(result.NormalizedTax.Value, amount.Currency);
        }

        candidate.SetAdditionalDetails(fees, tax, result.ReferenceId);

        return candidate;
    }
}
