using MediatR;

namespace MainLedger.Application.ExtractionCandidates.Commands;

/// <summary>
/// Command to reject an extraction candidate.
/// </summary>
public record RejectExtractionCandidateCommand(Guid CandidateId, Guid UserId, string Reason) : IRequest<Unit>;
