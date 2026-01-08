using MainLedger.Contracts.FinancialRecords;
using MediatR;

namespace MainLedger.Application.FinancialRecords.Queries;

/// <summary>
/// Query to get financial record statistics for a user.
/// </summary>
public record GetFinancialRecordStatisticsQuery(
    Guid UserId,
    DateTime? StartDate = null,
    DateTime? EndDate = null) : IRequest<FinancialRecordStatisticsDto>;
