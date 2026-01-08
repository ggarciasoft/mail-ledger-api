using MainLedger.Contracts.Common;
using MainLedger.Contracts.FinancialRecords;
using MainLedger.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.FinancialRecords.Queries;

public class GetFinancialRecordsQueryHandler : IRequestHandler<GetFinancialRecordsQuery, PaginatedResponse<FinancialRecordListItemDto>>
{
    private readonly IFinancialRecordRepository _recordRepository;
    private readonly ILogger<GetFinancialRecordsQueryHandler> _logger;

    public GetFinancialRecordsQueryHandler(
        IFinancialRecordRepository recordRepository,
        ILogger<GetFinancialRecordsQueryHandler> logger)
    {
        _recordRepository = recordRepository;
        _logger = logger;
    }

    public async Task<PaginatedResponse<FinancialRecordListItemDto>> Handle(
        GetFinancialRecordsQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Getting financial records for user {UserId}: Page={Page}, PageSize={PageSize}",
            request.UserId, request.Page, request.PageSize);

        // Validate pagination
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        // Get paginated records
        var (records, totalCount) = await _recordRepository.GetPagedAsync(
            request.UserId,
            request.StartDate,
            request.EndDate,
            request.MinAmount,
            request.MaxAmount,
            request.Merchant,
            request.Currency,
            request.SourceBank,
            page,
            pageSize,
            request.SortBy,
            request.SortOrder,
            cancellationToken);

        // Map to DTOs
        var items = records.Select(record => new FinancialRecordListItemDto
        {
            Id = record.Id,
            Type = record.Type.ToString(),
            Amount = record.Amount.Amount,
            Currency = record.Amount.Currency.ToString(),
            Merchant = record.Merchant,
            TransactionDate = record.TransactionDate,
            SourceAccount = record.SourceAccount?.Value,
            SourceBank = record.SourceBank?.Name,
            Direction = record.Direction.ToString(),
            ConfirmedAt = record.ConfirmedAt ?? record.CreatedAt
        }).ToList();

        return PaginatedResponse<FinancialRecordListItemDto>.Create(items, totalCount, page, pageSize);
    }
}
