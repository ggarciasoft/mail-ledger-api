using MainLedger.Contracts.Common;
using MainLedger.Contracts.FinancialRecords;
using MediatR;

namespace MainLedger.Application.FinancialRecords.Queries;

/// <summary>
/// Query to get paginated list of financial records with filtering.
/// </summary>
public record GetFinancialRecordsQuery(
    Guid UserId,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    decimal? MinAmount = null,
    decimal? MaxAmount = null,
    string? Merchant = null,
    string? Currency = null,
    string? SourceBank = null,
    int Page = 1,
    int PageSize = 20,
    string SortBy = "transactionDate",
    string SortOrder = "desc") : IRequest<PaginatedResponse<FinancialRecordListItemDto>>;
