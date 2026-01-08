using MainLedger.Contracts.Dashboard;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.Dashboard.Queries;

public class GetTopMerchantsQueryHandler : IRequestHandler<GetTopMerchantsQuery, TopMerchantsDto>
{
    private readonly IFinancialRecordRepository _recordRepository;
    private readonly ILogger<GetTopMerchantsQueryHandler> _logger;

    public GetTopMerchantsQueryHandler(
        IFinancialRecordRepository recordRepository,
        ILogger<GetTopMerchantsQueryHandler> logger)
    {
        _recordRepository = recordRepository;
        _logger = logger;
    }

    public async Task<TopMerchantsDto> Handle(GetTopMerchantsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting top {Limit} merchants for user {UserId}",
            request.Limit, request.UserId);

        // Get confirmed records in date range
        var records = await _recordRepository.GetConfirmedByUserIdAsync(
            request.UserId, request.StartDate, request.EndDate, cancellationToken);

        // Filter to outgoing transactions with merchants
        var spendingRecords = records
            .Where(r => r.Direction == TransactionDirection.Out && !string.IsNullOrWhiteSpace(r.Merchant))
            .ToList();

        var totalSpent = spendingRecords.Sum(r => r.Amount.Amount);

        // Group by merchant and calculate stats
        var merchantStats = spendingRecords
            .GroupBy(r => r.Merchant!)
            .Select(g => new MerchantStatsDto
            {
                Name = g.Key,
                TotalSpent = g.Sum(r => r.Amount.Amount),
                TransactionCount = g.Count(),
                Percentage = totalSpent > 0 ? (double)(g.Sum(r => r.Amount.Amount) / totalSpent * 100) : 0
            })
            .OrderByDescending(m => m.TotalSpent)
            .Take(request.Limit)
            .ToList();

        return new TopMerchantsDto
        {
            Merchants = merchantStats
        };
    }
}
