using MainLedger.Contracts.Common;
using MainLedger.Contracts.FinancialRecords;
using MainLedger.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.FinancialRecords.Queries;

public class GetFinancialRecordsQueryHandler
    : IRequestHandler<GetFinancialRecordsQuery, PaginatedResponse<FinancialRecordListItemDto>>
{
    private readonly IFinancialRecordRepository _recordRepository;
    private readonly ILogger<GetFinancialRecordsQueryHandler> _logger;

    public GetFinancialRecordsQueryHandler(
        IFinancialRecordRepository recordRepository,
        ILogger<GetFinancialRecordsQueryHandler> logger
    )
    {
        _recordRepository = recordRepository;
        _logger = logger;
    }

    public async Task<PaginatedResponse<FinancialRecordListItemDto>> Handle(
        GetFinancialRecordsQuery request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogInformation(
            "Getting financial records for user {UserId}: Page={Page}, PageSize={PageSize}",
            request.UserId,
            request.Page,
            request.PageSize
        );

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
            cancellationToken
        );

        // Map to DTOs
        var items = records
            .Select(record => new FinancialRecordListItemDto
            {
                Id = record.Id,
                EmailId = record.EmailMessageId,
                Type = record.Type.ToString(),
                Amount = record.Amount.Amount,
                Currency = record.Amount.Currency.ToString(),
                Direction = record.Direction.ToString(),
                Merchant = record.Merchant,
                SourceAccount = record.SourceAccount?.Value,
                SourceBank = record.SourceBank?.Name,
                TargetAccount = record.TargetAccount?.Value,
                TargetBank = record.TargetBank?.Name,
                TransactionDate = record.TransactionDate,
                TaxAmount = record.TaxAmount?.Amount,
                FeeAmount = record.FeeAmount?.Amount,
                Confidence = record.Confidence.Value,
                ExtractionVersion = record.ExtractionVersion,
                CreatedAt = record.CreatedAt,
                ConfirmedAt = record.ConfirmedAt ?? record.CreatedAt,
            })
            .ToList();

        return PaginatedResponse<FinancialRecordListItemDto>.Create(
            items,
            totalCount,
            page,
            pageSize
        );
    }
}
