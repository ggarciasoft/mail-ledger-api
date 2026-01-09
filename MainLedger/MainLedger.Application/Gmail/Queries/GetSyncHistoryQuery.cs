using MainLedger.Contracts.Gmail;
using MediatR;

namespace MainLedger.Application.Gmail.Queries;

/// <summary>
/// Query to get sync history for a user.
/// </summary>
public record GetSyncHistoryQuery(Guid UserId, int Limit = 10) : IRequest<SyncHistoryDto>;
