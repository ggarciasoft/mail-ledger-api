using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.ExtractionCandidates.Commands;

public class RejectExtractionCandidateCommandHandler : IRequestHandler<RejectExtractionCandidateCommand, Unit>
{
    private readonly IExtractionCandidateRepository _candidateRepository;
    private readonly IEmailMessageRepository _emailRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RejectExtractionCandidateCommandHandler> _logger;

    public RejectExtractionCandidateCommandHandler(
        IExtractionCandidateRepository candidateRepository,
        IEmailMessageRepository emailRepository,
        IUnitOfWork unitOfWork,
        ILogger<RejectExtractionCandidateCommandHandler> logger)
    {
        _candidateRepository = candidateRepository;
        _emailRepository = emailRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Unit> Handle(RejectExtractionCandidateCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Rejecting extraction candidate {CandidateId} for user {UserId}: {Reason}",
            request.CandidateId, request.UserId, request.Reason);

        // Get candidate
        var candidate = await _candidateRepository.GetByIdAsync(request.CandidateId, cancellationToken);
        if (candidate == null)
        {
            throw new KeyNotFoundException($"Extraction candidate {request.CandidateId} not found");
        }

        // Verify user authorization
        var email = await _emailRepository.GetByIdAsync(candidate.EmailMessageId, cancellationToken);
        if (email == null || email.UserId != request.UserId)
        {
            throw new UnauthorizedAccessException($"User {request.UserId} does not have access to candidate {request.CandidateId}");
        }

        // Verify status is Pending
        if (candidate.Status != RecordStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot reject candidate with status {candidate.Status}. Only Pending candidates can be rejected.");
        }

        // Reject the candidate
        candidate.Reject(request.Reason);

        // Save changes
        _candidateRepository.Update(candidate);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Rejected extraction candidate {CandidateId}", request.CandidateId);

        return Unit.Value;
    }
}
