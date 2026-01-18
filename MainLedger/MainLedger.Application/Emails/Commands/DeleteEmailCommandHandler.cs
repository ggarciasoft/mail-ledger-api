using MainLedger.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.Emails.Commands;

/// <summary>
/// Handler for deleting a single email.
/// </summary>
public class DeleteEmailCommandHandler : IRequestHandler<DeleteEmailCommand, bool>
{
    private readonly IEmailMessageRepository _emailRepository;
    private readonly IExtractionCandidateRepository _candidateRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteEmailCommandHandler> _logger;

    public DeleteEmailCommandHandler(
        IEmailMessageRepository emailRepository,
        IExtractionCandidateRepository candidateRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteEmailCommandHandler> logger
    )
    {
        _emailRepository = emailRepository;
        _candidateRepository = candidateRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteEmailCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Deleting email {EmailId} for user {UserId}",
            request.EmailId,
            request.UserId
        );

        // Get email
        var email = await _emailRepository.GetByIdAsync(request.EmailId, cancellationToken);
        if (email == null)
        {
            _logger.LogWarning("Email {EmailId} not found", request.EmailId);
            throw new InvalidOperationException("Email not found");
        }

        // Verify ownership
        if (email.UserId != request.UserId)
        {
            _logger.LogWarning(
                "User {UserId} attempted to delete email {EmailId} owned by {OwnerId}",
                request.UserId,
                request.EmailId,
                email.UserId
            );
            throw new UnauthorizedAccessException(
                "You do not have permission to delete this email"
            );
        }

        // Check if email has extraction candidates
        var hasCandidates = await _candidateRepository.HasCandidatesForEmailAsync(
            request.EmailId,
            cancellationToken
        );
        if (hasCandidates)
        {
            _logger.LogWarning(
                "Cannot delete email {EmailId} - has extraction candidates",
                request.EmailId
            );
            throw new InvalidOperationException(
                "Cannot delete email that has extraction candidates. Please delete or process the candidates first."
            );
        }

        // Delete email
        _emailRepository.Delete(email);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully deleted email {EmailId}", request.EmailId);
        return true;
    }
}
