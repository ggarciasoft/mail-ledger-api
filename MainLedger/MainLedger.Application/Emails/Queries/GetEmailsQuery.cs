using MainLedger.Contracts.Common;
using MainLedger.Contracts.Emails;
using MainLedger.Domain.Enums;
using MediatR;

namespace MainLedger.Application.Emails.Queries;

/// <summary>
/// Query to get paginated list of emails with filtering.
/// </summary>
public record GetEmailsQuery(
    Guid UserId,
    EmailProcessingStatus? Status = null,
    bool? IsFinancial = null,
    int Page = 1,
    int PageSize = 20,
    string SortBy = "receivedAt",
    string SortOrder = "desc") : IRequest<PaginatedResponse<EmailListItemDto>>;
