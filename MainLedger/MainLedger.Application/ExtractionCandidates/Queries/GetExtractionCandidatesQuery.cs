using MainLedger.Contracts.Common;
using MainLedger.Contracts.ExtractionCandidates;
using MainLedger.Domain.Enums;
using MediatR;

namespace MainLedger.Application.ExtractionCandidates.Queries;

/// <summary>
/// Query to get paginated list of extraction candidates.
/// </summary>
public record GetExtractionCandidatesQuery(
    Guid UserId,
    RecordStatus? Status = null,
    int Page = 1,
    int PageSize = 20,
    string SortBy = "createdAt",
    string SortOrder = "desc") : IRequest<PaginatedResponse<ExtractionCandidateListItemDto>>;
