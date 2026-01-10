using MainLedger.Contracts.Common;
using MainLedger.Contracts.ExtractionCandidates;
using MainLedger.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.ExtractionCandidates.Queries;

public class GetExtractionCandidatesQueryHandler
    : IRequestHandler<
        GetExtractionCandidatesQuery,
        PaginatedResponse<ExtractionCandidateListItemDto>
    >
{
    private readonly IExtractionCandidateRepository _candidateRepository;
    private readonly IEmailMessageRepository _emailRepository;
    private readonly ILogger<GetExtractionCandidatesQueryHandler> _logger;

    public GetExtractionCandidatesQueryHandler(
        IExtractionCandidateRepository candidateRepository,
        IEmailMessageRepository emailRepository,
        ILogger<GetExtractionCandidatesQueryHandler> logger
    )
    {
        _candidateRepository = candidateRepository;
        _emailRepository = emailRepository;
        _logger = logger;
    }

    public async Task<PaginatedResponse<ExtractionCandidateListItemDto>> Handle(
        GetExtractionCandidatesQuery request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogInformation(
            "Getting extraction candidates for user {UserId}: Page={Page}, PageSize={PageSize}, Status={Status}",
            request.UserId,
            request.Page,
            request.PageSize,
            request.Status
        );

        // Validate pagination
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        // Get paginated candidates
        var (candidates, totalCount) = await _candidateRepository.GetPagedAsync(
            request.UserId,
            request.Status,
            page,
            pageSize,
            request.SortBy,
            request.SortOrder,
            cancellationToken
        );

        // Get email details for all candidates
        var emailIds = candidates.Select(c => c.EmailMessageId).Distinct().ToList();
        var emails = new Dictionary<Guid, Domain.Entities.EmailMessage>();

        foreach (var emailId in emailIds)
        {
            var email = await _emailRepository.GetByIdAsync(emailId, cancellationToken);
            if (email != null)
            {
                emails[emailId] = email;
            }
        }

        // Map to DTOs
        var items = candidates
            .Select(candidate => new ExtractionCandidateListItemDto
            {
                Id = candidate.Id,
                EmailId = candidate.EmailMessageId,
                EmailSubject =
                    emails.GetValueOrDefault(candidate.EmailMessageId)?.Subject ?? "Unknown",
                EmailFrom =
                    emails.GetValueOrDefault(candidate.EmailMessageId)?.From.Value ?? "Unknown",
                EmailReceivedAt =
                    emails.GetValueOrDefault(candidate.EmailMessageId)?.ReceivedAt
                    ?? DateTime.MinValue,
                Amount = candidate.Amount?.Amount,
                Currency = candidate.Amount?.Currency.ToString(),
                Merchant = candidate.Merchant,
                TransactionDate = candidate.TransactionDate,
                SourceAccount = candidate.SourceAccount?.Value,
                SourceBank = candidate.SourceBank?.Name,
                Status = candidate.Status.ToString(),
                Confidence = CalculateOverallConfidence(candidate),
                CreatedAt = candidate.CreatedAt,
            })
            .ToList();

        return PaginatedResponse<ExtractionCandidateListItemDto>.Create(
            items,
            totalCount,
            page,
            pageSize
        );
    }

    private double? CalculateOverallConfidence(Domain.Entities.ExtractionCandidate candidate)
    {
        var confidences = new List<double>();

        if (candidate.AmountConfidence != null)
            confidences.Add(candidate.AmountConfidence.Value);
        if (candidate.DateConfidence != null)
            confidences.Add(candidate.DateConfidence.Value);
        if (candidate.MerchantConfidence != null)
            confidences.Add(candidate.MerchantConfidence.Value);

        return confidences.Any() ? confidences.Average() : null;
    }
}
