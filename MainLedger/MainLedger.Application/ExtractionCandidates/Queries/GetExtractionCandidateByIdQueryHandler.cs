using MainLedger.Contracts.ExtractionCandidates;
using MainLedger.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.ExtractionCandidates.Queries;

public class GetExtractionCandidateByIdQueryHandler
    : IRequestHandler<GetExtractionCandidateByIdQuery, ExtractionCandidateDto?>
{
    private readonly IExtractionCandidateRepository _candidateRepository;
    private readonly IEmailMessageRepository _emailRepository;
    private readonly ILogger<GetExtractionCandidateByIdQueryHandler> _logger;

    public GetExtractionCandidateByIdQueryHandler(
        IExtractionCandidateRepository candidateRepository,
        IEmailMessageRepository emailRepository,
        ILogger<GetExtractionCandidateByIdQueryHandler> logger
    )
    {
        _candidateRepository = candidateRepository;
        _emailRepository = emailRepository;
        _logger = logger;
    }

    public async Task<ExtractionCandidateDto?> Handle(
        GetExtractionCandidateByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogInformation(
            "Getting extraction candidate {CandidateId} for user {UserId}",
            request.CandidateId,
            request.UserId
        );

        var candidate = await _candidateRepository.GetByIdAsync(
            request.CandidateId,
            cancellationToken
        );

        if (candidate == null)
        {
            _logger.LogWarning("Extraction candidate {CandidateId} not found", request.CandidateId);
            return null;
        }

        // Get email to verify user ownership and get subject
        var email = await _emailRepository.GetByIdAsync(
            candidate.EmailMessageId,
            cancellationToken
        );

        if (email == null || email.UserId != request.UserId)
        {
            _logger.LogWarning(
                "Access denied: Extraction candidate {CandidateId} does not belong to user {UserId}",
                request.CandidateId,
                request.UserId
            );
            return null;
        }

        return new ExtractionCandidateDto
        {
            Id = candidate.Id,
            EmailId = candidate.EmailMessageId,
            EmailSubject = email.Subject,
            EmailFrom = email.From.Value,
            EmailReceivedAt = email.ReceivedAt,
            EmailMessageId = email.MessageId,
            Amount = candidate.Amount?.Amount,
            Currency = candidate.Amount?.Currency.ToString(),
            Merchant = candidate.Merchant,
            TransactionDate = candidate.TransactionDate,
            SourceAccount = candidate.SourceAccount?.Value,
            TargetAccount = candidate.TargetAccount?.Value,
            SourceBank = candidate.SourceBank?.Name,
            TargetBank = candidate.TargetBank?.Name,
            Fees = candidate.Fees?.Amount,
            Tax = candidate.Tax?.Amount,
            ReferenceId = candidate.ReferenceId,
            AmountConfidence = candidate.AmountConfidence?.Value,
            DateConfidence = candidate.DateConfidence?.Value,
            MerchantConfidence = candidate.MerchantConfidence?.Value,
            Status = candidate.Status.ToString(),
            CreatedAt = candidate.CreatedAt,
            ConfirmedAt = candidate.ConfirmedAt,
            RejectionReason = candidate.RejectionReason,
        };
    }
}
