using MainLedger.Application.Common.Interfaces;
using MainLedger.Contracts.ExtractionCandidates;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.ExtractionCandidates.Commands;

/// <summary>
/// Handler for bulk rejecting extraction candidates.
/// </summary>
public class BulkRejectExtractionCandidatesCommandHandler
    : IRequestHandler<BulkRejectExtractionCandidatesCommand, BulkOperationResponse>
{
    private readonly IExtractionCandidateRepository _candidateRepository;
    private readonly IEmailMessageRepository _emailRepository;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BulkRejectExtractionCandidatesCommandHandler> _logger;

    public BulkRejectExtractionCandidatesCommandHandler(
        IExtractionCandidateRepository candidateRepository,
        IEmailMessageRepository emailRepository,
        ISubscriptionService subscriptionService,
        IUnitOfWork unitOfWork,
        ILogger<BulkRejectExtractionCandidatesCommandHandler> logger
    )
    {
        _candidateRepository = candidateRepository;
        _emailRepository = emailRepository;
        _subscriptionService = subscriptionService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<BulkOperationResponse> Handle(
        BulkRejectExtractionCandidatesCommand request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogInformation(
            "Bulk rejecting {Count} extraction candidates for user {UserId}",
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
                            Error = $"Cannot reject candidate with status {candidate.Status}",
                        }
                    );
                    continue;
                }

                // Reject candidate
                candidate.Reject(request.Reason ?? "Bulk rejection");

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                succeeded++;

                _logger.LogInformation("Rejected extraction candidate {CandidateId}", candidateId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reject candidate {CandidateId}", candidateId);
                errors.Add(
                    new BulkOperationError { CandidateId = candidateId, Error = ex.Message }
                );
            }
        }

        _logger.LogInformation(
            "Bulk reject completed: {Succeeded} succeeded, {Failed} failed",
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
}
