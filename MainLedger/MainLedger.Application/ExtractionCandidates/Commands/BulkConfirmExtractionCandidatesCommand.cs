using MainLedger.Contracts.ExtractionCandidates;
using MediatR;

namespace MainLedger.Application.ExtractionCandidates.Commands;

/// <summary>
/// Command to bulk confirm multiple extraction candidates.
/// </summary>
public record BulkConfirmExtractionCandidatesCommand(List<Guid> CandidateIds, Guid UserId)
    : IRequest<BulkOperationResponse>;
