using MainLedger.Contracts.FinancialRecords;
using MainLedger.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.FinancialRecords.Queries;

public class GetFinancialRecordStatisticsQueryHandler : IRequestHandler<GetFinancialRecordStatisticsQuery, FinancialRecordStatisticsDto>
{
    private readonly IFinancialRecordRepository _recordRepository;
    private readonly ILogger<GetFinancialRecordStatisticsQueryHandler> _logger;

    public GetFinancialRecordStatisticsQueryHandler(
        IFinancialRecordRepository recordRepository,
        ILogger<GetFinancialRecordStatisticsQueryHandler> logger)
    {
        _recordRepository = recordRepository;
        _logger = logger;
    }

    public async Task<FinancialRecordStatisticsDto> Handle(
        GetFinancialRecordStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Getting financial record statistics for user {UserId}",
            request.UserId);

        var statistics = await _recordRepository.GetStatisticsAsync(
            request.UserId,
            request.StartDate,
            request.EndDate,
            cancellationToken);

        return new FinancialRecordStatisticsDto
        {
            TotalRecords = statistics.TotalRecords,
            TotalAmount = statistics.TotalAmount,
            Currency = "USD", // TODO: Support multiple currencies
            AverageAmount = statistics.AverageAmount,
            ByType = statistics.ByType,
            ByBank = statistics.ByBank,
            MonthlyTrend = statistics.MonthlyTrend.Select(t => new MonthlyTrendDto
            {
                Month = t.Month,
                Count = t.Count,
                Total = t.Total
            }).ToList()
        };
    }
}
