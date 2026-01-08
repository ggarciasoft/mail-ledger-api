using MainLedger.Contracts.Dashboard;
using MediatR;

namespace MainLedger.Application.Dashboard.Queries;

/// <summary>
/// Query to get spending trends over time.
/// </summary>
public record GetSpendingTrendsQuery(
    Guid UserId,
    string Period = "month",
    string GroupBy = "day") : IRequest<SpendingTrendsDto>;
