using MainLedger.Contracts.Processing;
using MediatR;

namespace MainLedger.Application.Processing.Queries;

/// <summary>
/// Query to get processing status for a user.
/// </summary>
public record GetProcessingStatusQuery(Guid UserId) : IRequest<ProcessingStatusDto>;
