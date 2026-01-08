using MediatR;

namespace MainLedger.Application.ExtractionCandidates.Commands;

/// <summary>
/// Command to update an extraction candidate before confirmation.
/// </summary>
public record UpdateExtractionCandidateCommand(
    Guid CandidateId,
    Guid UserId,
    decimal? Amount,
    string? Currency,
    string? Merchant,
    DateTime? TransactionDate,
    string? SourceAccount,
    string? TargetAccount,
    string? SourceBank,
    string? TargetBank,
    decimal? Fees,
    decimal? Tax,
    string? ReferenceId) : IRequest<Unit>;
