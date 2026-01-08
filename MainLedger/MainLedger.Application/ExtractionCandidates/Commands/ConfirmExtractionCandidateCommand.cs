using MediatR;

namespace MainLedger.Application.ExtractionCandidates.Commands;

/// <summary>
/// Command to confirm an extraction candidate and create a financial record.
/// </summary>
public record ConfirmExtractionCandidateCommand(Guid CandidateId, Guid UserId) : IRequest<Guid>;
