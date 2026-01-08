using MainLedger.Contracts.Dashboard;
using MediatR;

namespace MainLedger.Application.Dashboard.Queries;

/// <summary>
/// Query to get dashboard overview with aggregated statistics.
/// </summary>
public record GetDashboardOverviewQuery(Guid UserId) : IRequest<DashboardOverviewDto>;
