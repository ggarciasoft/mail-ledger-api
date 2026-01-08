using MainLedger.Contracts.Processing;
using MediatR;

namespace MainLedger.Application.Processing.Commands;

/// <summary>
/// Command to trigger batch extraction for a user.
/// </summary>
public record TriggerExtractionCommand(Guid UserId, int BatchSize = 20) : IRequest<TriggerJobResponseDto>;
