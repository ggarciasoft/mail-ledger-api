using MainLedger.Contracts.FinancialRecords;
using MediatR;

namespace MainLedger.Application.FinancialRecords.Queries;

/// <summary>
/// Query to get a single financial record by ID.
/// </summary>
public record GetFinancialRecordByIdQuery(Guid RecordId, Guid UserId) : IRequest<FinancialRecordDto?>;
