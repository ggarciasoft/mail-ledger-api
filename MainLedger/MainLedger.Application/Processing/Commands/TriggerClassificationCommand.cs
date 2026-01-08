using MainLedger.Contracts.Processing;
using MediatR;

namespace MainLedger.Application.Processing.Commands;

/// <summary>
/// Command to trigger batch classification for a user.
/// </summary>
public record TriggerClassificationCommand(Guid UserId, int BatchSize = 20) : IRequest<TriggerJobResponseDto>;
