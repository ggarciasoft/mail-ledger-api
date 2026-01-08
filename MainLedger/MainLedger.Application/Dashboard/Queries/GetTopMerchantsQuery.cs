using MainLedger.Contracts.Dashboard;
using MediatR;

namespace MainLedger.Application.Dashboard.Queries;

/// <summary>
/// Query to get top merchants by spending.
/// </summary>
public record GetTopMerchantsQuery(
    Guid UserId,
    int Limit = 10,
    DateTime? StartDate = null,
    DateTime? EndDate = null) : IRequest<TopMerchantsDto>;
