using MainLedger.Contracts.Emails;
using MainLedger.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.Emails.Queries;

public class GetEmailByIdQueryHandler : IRequestHandler<GetEmailByIdQuery, EmailDto?>
{
    private readonly IEmailMessageRepository _emailRepository;
    private readonly ILogger<GetEmailByIdQueryHandler> _logger;

    public GetEmailByIdQueryHandler(
        IEmailMessageRepository emailRepository,
        ILogger<GetEmailByIdQueryHandler> logger)
    {
        _emailRepository = emailRepository;
        _logger = logger;
    }

    public async Task<EmailDto?> Handle(GetEmailByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting email {EmailId} for user {UserId}", request.EmailId, request.UserId);

        var email = await _emailRepository.GetByIdAsync(request.EmailId, cancellationToken);

        if (email == null || email.UserId != request.UserId)
        {
            _logger.LogWarning("Email {EmailId} not found or access denied for user {UserId}", request.EmailId, request.UserId);
            return null;
        }

        return new EmailDto
        {
            Id = email.Id,
            MessageId = email.MessageId,
            Subject = email.Subject,
            From = email.From.Value,
            ReceivedAt = email.ReceivedAt,
            BodyText = email.BodyText,
            ProcessingStatus = email.ProcessingStatus.ToString(),
            ProcessingError = email.ProcessingError,
            Directive = email.Directive?.ToString(),
            DirectiveReason = email.DirectiveReason,
            IsFinancial = email.IsFinancial,
            Category = email.Category?.ToString(),
            ClassificationConfidence = email.ClassificationConfidence?.Value,
            ClassifiedAt = email.ClassifiedAt,
            CreatedAt = email.CreatedAt
        };
    }
}
