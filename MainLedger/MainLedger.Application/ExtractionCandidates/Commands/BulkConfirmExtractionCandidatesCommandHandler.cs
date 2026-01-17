using MainLedger.Application.Common.Interfaces;
using MainLedger.Contracts.ExtractionCandidates;
using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using MainLedger.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.ExtractionCandidates.Commands;

/// <summary>
/// Handler for bulk confirming extraction candidates.
/// </summary>
public class BulkConfirmExtractionCandidatesCommandHandler
    : IRequestHandler<BulkConfirmExtractionCandidatesCommand, BulkOperationResponse>
{
    private readonly IExtractionCandidateRepository _candidateRepository;
    private readonly IEmailMessageRepository _emailRepository;
    private readonly IFinancialRecordRepository _recordRepository;
    private readonly IExtractionVersionRepository _versionRepository;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BulkConfirmExtractionCandidatesCommandHandler> _logger;

    public BulkConfirmExtractionCandidatesCommandHandler(
        IExtractionCandidateRepository candidateRepository,
        IEmailMessageRepository emailRepository,
        IFinancialRecordRepository recordRepository,
        IExtractionVersionRepository versionRepository,
        ISubscriptionService subscriptionService,
        IUnitOfWork unitOfWork,
        ILogger<BulkConfirmExtractionCandidatesCommandHandler> logger
    )
    {
        _candidateRepository = candidateRepository;
        _emailRepository = emailRepository;
        _recordRepository = recordRepository;
        _versionRepository = versionRepository;
        _subscriptionService = subscriptionService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<BulkOperationResponse> Handle(
        BulkConfirmExtractionCandidatesCommand request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogInformation(
            "Bulk confirming {Count} extraction candidates for user {UserId}",
            request.CandidateIds.Count,
            request.UserId
        );

        // Validate request
        if (request.CandidateIds.Count == 0)
        {
            return new BulkOperationResponse
            {
                TotalRequested = 0,
                Succeeded = 0,
                Failed = 0,
            };
        }

        if (request.CandidateIds.Count > 100)
        {
            throw new InvalidOperationException("Cannot process more than 100 candidates at once");
        }

        // Check subscription limits
        var canUseBulkOperations = await _subscriptionService.CanUseBulkOperationsAsync(
            request.UserId,
            cancellationToken
        );
        if (!canUseBulkOperations)
        {
            throw new InvalidOperationException(
                "Bulk operations are not available on your current subscription plan. Please upgrade to use this feature."
            );
        }

        var succeeded = 0;
        var errors = new List<BulkOperationError>();

        // Process each candidate independently
        foreach (var candidateId in request.CandidateIds)
        {
            try
            {
                var candidate = await _candidateRepository.GetByIdAsync(
                    candidateId,
                    cancellationToken
                );

                if (candidate == null)
                {
                    errors.Add(
                        new BulkOperationError
                        {
                            CandidateId = candidateId,
                            Error = "Candidate not found",
                        }
                    );
                    continue;
                }

                // Verify ownership
                var email = await _emailRepository.GetByIdAsync(
                    candidate.EmailMessageId,
                    cancellationToken
                );
                if (email == null || email.UserId != request.UserId)
                {
                    errors.Add(
                        new BulkOperationError
                        {
                            CandidateId = candidateId,
                            Error = "Access denied",
                        }
                    );
                    continue;
                }

                // Check status
                if (candidate.Status != RecordStatus.Pending)
                {
                    errors.Add(
                        new BulkOperationError
                        {
                            CandidateId = candidateId,
                            Error = $"Cannot confirm candidate with status {candidate.Status}",
                        }
                    );
                    continue;
                }

                // Validate amount
                if (candidate.Amount == null)
                {
                    errors.Add(
                        new BulkOperationError
                        {
                            CandidateId = candidateId,
                            Error = "Cannot create financial record without amount",
                        }
                    );
                    continue;
                }

                // Get extraction version
                var version = await _versionRepository.GetByIdAsync(
                    candidate.ExtractionVersionId,
                    cancellationToken
                );
                if (version == null)
                {
                    errors.Add(
                        new BulkOperationError
                        {
                            CandidateId = candidateId,
                            Error = $"Extraction version {candidate.ExtractionVersionId} not found",
                        }
                    );
                    continue;
                }

                // Confirm candidate
                candidate.Confirm();

                // Calculate overall confidence
                var confidenceValue = CalculateOverallConfidence(candidate);
                var confidence = Confidence.Create(confidenceValue);

                // Determine transaction type and direction
                var transactionType = DetermineTransactionType(candidate);
                var direction = DetermineDirection(candidate);

                // Create financial record
                var financialRecord = FinancialRecord.Create(
                    userId: request.UserId,
                    emailMessageId: candidate.EmailMessageId,
                    type: transactionType,
                    amount: candidate.Amount,
                    direction: direction,
                    confidence: confidence,
                    extractionVersion: version.Version,
                    transactionDate: candidate.TransactionDate ?? email.ReceivedAt,
                    merchant: candidate.Merchant,
                    sourceAccount: candidate.SourceAccount,
                    sourceBank: candidate.SourceBank,
                    targetAccount: candidate.TargetAccount,
                    targetBank: candidate.TargetBank,
                    taxAmount: candidate.Tax,
                    feeAmount: candidate.Fees
                );

                // Confirm the financial record immediately
                financialRecord.Confirm();

                await _recordRepository.AddAsync(financialRecord, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                succeeded++;

                _logger.LogInformation(
                    "Confirmed extraction candidate {CandidateId}, created financial record {RecordId}",
                    candidateId,
                    financialRecord.Id
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to confirm candidate {CandidateId}", candidateId);
                errors.Add(
                    new BulkOperationError { CandidateId = candidateId, Error = ex.Message }
                );
            }
        }

        _logger.LogInformation(
            "Bulk confirm completed: {Succeeded} succeeded, {Failed} failed",
            succeeded,
            errors.Count
        );

        return new BulkOperationResponse
        {
            TotalRequested = request.CandidateIds.Count,
            Succeeded = succeeded,
            Failed = errors.Count,
            Errors = errors,
        };
    }

    private double CalculateOverallConfidence(ExtractionCandidate candidate)
    {
        var confidences = new List<double>();

        if (candidate.AmountConfidence != null)
            confidences.Add(candidate.AmountConfidence.Value);
        if (candidate.DateConfidence != null)
            confidences.Add(candidate.DateConfidence.Value);
        if (candidate.MerchantConfidence != null)
            confidences.Add(candidate.MerchantConfidence.Value);

        return confidences.Any() ? confidences.Average() : 0.5;
    }

    private TransactionType DetermineTransactionType(ExtractionCandidate candidate)
    {
        if (!string.IsNullOrWhiteSpace(candidate.Merchant))
            return TransactionType.Payment;

        if (candidate.SourceAccount != null && candidate.TargetAccount != null)
            return TransactionType.Transfer;

        return TransactionType.Payment;
    }

    private TransactionDirection DetermineDirection(ExtractionCandidate candidate)
    {
        // Simplified logic - assume payments are outgoing
        return TransactionDirection.Out;
    }
}
