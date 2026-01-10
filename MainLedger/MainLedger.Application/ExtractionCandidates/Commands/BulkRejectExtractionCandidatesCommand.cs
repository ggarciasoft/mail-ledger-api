using MainLedger.Contracts.ExtractionCandidates;
using MediatR;

namespace MainLedger.Application.ExtractionCandidates.Commands;

/// <summary>
/// Command to bulk reject multiple extraction candidates.
/// </summary>
public record BulkRejectExtractionCandidatesCommand(
    List<Guid> CandidateIds,
    Guid UserId,
    string? Reason
) : IRequest<BulkOperationResponse>;
