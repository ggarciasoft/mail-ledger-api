using MediatR;

namespace MainLedger.Application.ExtractionCandidates.Commands;

/// <summary>
/// Command to confirm an extraction candidate and create a financial record.
/// </summary>
public record ConfirmExtractionCandidateCommand(
    Guid CandidateId,
    Guid UserId,
    string? Merchant = null,
    string? Category = null
) : IRequest<Guid>;
