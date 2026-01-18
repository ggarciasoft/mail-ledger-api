using MainLedger.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.Emails.Commands;

/// <summary>
/// Handler for bulk deleting emails.
/// </summary>
public class BulkDeleteEmailsCommandHandler
    : IRequestHandler<BulkDeleteEmailsCommand, BulkDeleteResult>
{
    private readonly IEmailMessageRepository _emailRepository;
    private readonly IExtractionCandidateRepository _candidateRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BulkDeleteEmailsCommandHandler> _logger;

    public BulkDeleteEmailsCommandHandler(
        IEmailMessageRepository emailRepository,
        IExtractionCandidateRepository candidateRepository,
        IUnitOfWork _unitOfWork,
        ILogger<BulkDeleteEmailsCommandHandler> logger
    )
    {
        _emailRepository = emailRepository;
        _candidateRepository = candidateRepository;
        this._unitOfWork = _unitOfWork;
        _logger = logger;
    }

    public async Task<BulkDeleteResult> Handle(
        BulkDeleteEmailsCommand request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogInformation(
            "Bulk deleting {Count} emails for user {UserId}",
            request.EmailIds.Count,
            request.UserId
        );

        if (request.EmailIds.Count == 0)
        {
            return new BulkDeleteResult
            {
                TotalRequested = 0,
                Succeeded = 0,
                Failed = 0,
            };
        }

        if (request.EmailIds.Count > 100)
        {
            throw new InvalidOperationException("Cannot delete more than 100 emails at once");
        }

        var succeeded = 0;
        var errors = new List<BulkDeleteError>();

        // Process each email independently
        foreach (var emailId in request.EmailIds)
        {
            try
            {
                var email = await _emailRepository.GetByIdAsync(emailId, cancellationToken);

                if (email == null)
                {
                    errors.Add(
                        new BulkDeleteError { EmailId = emailId, Error = "Email not found" }
                    );
                    continue;
                }

                // Verify ownership
                if (email.UserId != request.UserId)
                {
                    errors.Add(new BulkDeleteError { EmailId = emailId, Error = "Access denied" });
                    continue;
                }

                // Check if email has extraction candidates
                var hasCandidates = await _candidateRepository.HasCandidatesForEmailAsync(
                    emailId,
                    cancellationToken
                );
                if (hasCandidates)
                {
                    errors.Add(
                        new BulkDeleteError
                        {
                            EmailId = emailId,
                            Error = "Email has extraction candidates",
                        }
                    );
                    continue;
                }

                // Delete email
                _emailRepository.Delete(email);
                succeeded++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete email {EmailId}", emailId);
                errors.Add(new BulkDeleteError { EmailId = emailId, Error = ex.Message });
            }
        }

        // Save all changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Bulk delete completed: {Succeeded} succeeded, {Failed} failed",
            succeeded,
            errors.Count
        );

        return new BulkDeleteResult
        {
            TotalRequested = request.EmailIds.Count,
            Succeeded = succeeded,
            Failed = errors.Count,
            Errors = errors,
        };
    }
}
