using MainLedger.Contracts.Common;
using MainLedger.Contracts.Emails;
using MainLedger.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.Emails.Queries;

public class GetEmailsQueryHandler
    : IRequestHandler<GetEmailsQuery, PaginatedResponse<EmailListItemDto>>
{
    private readonly IEmailMessageRepository _emailRepository;
    private readonly ILogger<GetEmailsQueryHandler> _logger;

    public GetEmailsQueryHandler(
        IEmailMessageRepository emailRepository,
        ILogger<GetEmailsQueryHandler> logger
    )
    {
        _emailRepository = emailRepository;
        _logger = logger;
    }

    public async Task<PaginatedResponse<EmailListItemDto>> Handle(
        GetEmailsQuery request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogInformation(
            "Getting emails for user {UserId}: Page={Page}, PageSize={PageSize}, Status={Status}, IsFinancial={IsFinancial}",
            request.UserId,
            request.Page,
            request.PageSize,
            request.Status,
            request.IsFinancial
        );

        // Validate pagination parameters
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        // Get paginated emails
        var (emails, totalCount) = await _emailRepository.GetPagedAsync(
            request.UserId,
            request.Status,
            request.IsFinancial,
            page,
            pageSize,
            request.SortBy,
            request.SortOrder,
            cancellationToken
        );

        // Map to DTOs
        var items = emails
            .Select(email => new EmailListItemDto
            {
                Id = email.Id,
                MessageId = email.MessageId,
                ThreadId = email.ThreadId,
                Provider = email.Provider.ToString(),
                Subject = email.Subject,
                From = email.From.Value,
                ReceivedAt = email.ReceivedAt,
                CreatedAt = email.CreatedAt,
                ProcessingStatus = email.ProcessingStatus.ToString(),
                ProcessingError = email.ProcessingError,
                IsFinancial = email.IsFinancial,
                Category = email.Category?.ToString(),
                ClassificationConfidence = email.ClassificationConfidence?.Value,
                HasExtractionCandidate = false, // TODO: Check if extraction candidate exists
            })
            .ToList();

        return PaginatedResponse<EmailListItemDto>.Create(items, totalCount, page, pageSize);
    }
}
