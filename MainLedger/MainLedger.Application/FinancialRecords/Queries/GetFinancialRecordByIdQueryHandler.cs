using MainLedger.Contracts.FinancialRecords;
using MainLedger.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.FinancialRecords.Queries;

public class GetFinancialRecordByIdQueryHandler : IRequestHandler<GetFinancialRecordByIdQuery, FinancialRecordDto?>
{
    private readonly IFinancialRecordRepository _recordRepository;
    private readonly ILogger<GetFinancialRecordByIdQueryHandler> _logger;

    public GetFinancialRecordByIdQueryHandler(
        IFinancialRecordRepository recordRepository,
        ILogger<GetFinancialRecordByIdQueryHandler> logger)
    {
        _recordRepository = recordRepository;
        _logger = logger;
    }

    public async Task<FinancialRecordDto?> Handle(
        GetFinancialRecordByIdQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Getting financial record {RecordId} for user {UserId}",
            request.RecordId, request.UserId);

        var record = await _recordRepository.GetByIdAsync(request.RecordId, cancellationToken);

        if (record == null || record.UserId != request.UserId)
        {
            _logger.LogWarning(
                "Financial record {RecordId} not found or access denied for user {UserId}",
                request.RecordId, request.UserId);
            return null;
        }

        return new FinancialRecordDto
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
            ConfirmedAt = record.ConfirmedAt ?? record.CreatedAt
        };
    }
}
