using MainLedger.Contracts.Dashboard;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.Dashboard.Queries;

public class GetSpendingTrendsQueryHandler : IRequestHandler<GetSpendingTrendsQuery, SpendingTrendsDto>
{
    private readonly IFinancialRecordRepository _recordRepository;
    private readonly ILogger<GetSpendingTrendsQueryHandler> _logger;

    public GetSpendingTrendsQueryHandler(
        IFinancialRecordRepository recordRepository,
        ILogger<GetSpendingTrendsQueryHandler> logger)
    {
        _recordRepository = recordRepository;
        _logger = logger;
    }

    public async Task<SpendingTrendsDto> Handle(GetSpendingTrendsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting spending trends for user {UserId}: Period={Period}, GroupBy={GroupBy}",
            request.UserId, request.Period, request.GroupBy);

        // Calculate date range based on period
        var endDate = DateTime.UtcNow.Date;
        var startDate = request.Period.ToLowerInvariant() switch
        {
            "week" => endDate.AddDays(-7),
            "year" => endDate.AddYears(-1),
            _ => endDate.AddMonths(-1) // default to month
        };

        // Get confirmed records in date range
        var records = await _recordRepository.GetConfirmedByUserIdAsync(
            request.UserId, startDate, endDate, cancellationToken);

        // Filter to outgoing transactions only (spending)
        var spendingRecords = records.Where(r => r.Direction == TransactionDirection.Out).ToList();

        // Group by date based on groupBy parameter
        var groupedData = request.GroupBy.ToLowerInvariant() switch
        {
            "week" => GroupByWeek(spendingRecords),
            "month" => GroupByMonth(spendingRecords),
            _ => GroupByDay(spendingRecords)
        };

        var totalSpent = spendingRecords.Sum(r => r.Amount.Amount);
        var dayCount = (endDate - startDate).Days + 1;
        var averageDaily = dayCount > 0 ? totalSpent / dayCount : 0;

        return new SpendingTrendsDto
        {
            Period = request.Period,
            Data = groupedData,
            TotalSpent = totalSpent,
            AverageDaily = averageDaily
        };
    }

    private List<DailySpendingDto> GroupByDay(List<Domain.Entities.FinancialRecord> records)
    {
        return records
            .GroupBy(r => r.TransactionDate.Date)
            .Select(g => new DailySpendingDto
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                TotalSpent = g.Sum(r => r.Amount.Amount),
                TransactionCount = g.Count()
            })
            .OrderBy(d => d.Date)
            .ToList();
    }

    private List<DailySpendingDto> GroupByWeek(List<Domain.Entities.FinancialRecord> records)
    {
        return records
            .GroupBy(r => GetWeekStart(r.TransactionDate))
            .Select(g => new DailySpendingDto
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                TotalSpent = g.Sum(r => r.Amount.Amount),
                TransactionCount = g.Count()
            })
            .OrderBy(d => d.Date)
            .ToList();
    }

    private List<DailySpendingDto> GroupByMonth(List<Domain.Entities.FinancialRecord> records)
    {
        return records
            .GroupBy(r => new DateTime(r.TransactionDate.Year, r.TransactionDate.Month, 1))
            .Select(g => new DailySpendingDto
            {
                Date = g.Key.ToString("yyyy-MM"),
                TotalSpent = g.Sum(r => r.Amount.Amount),
                TransactionCount = g.Count()
            })
            .OrderBy(d => d.Date)
            .ToList();
    }

    private DateTime GetWeekStart(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-1 * diff).Date;
    }
}
