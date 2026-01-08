using MainLedger.Contracts.ExtractionCandidates;
using MediatR;

namespace MainLedger.Application.ExtractionCandidates.Queries;

/// <summary>
/// Query to get a single extraction candidate by ID.
/// </summary>
public record GetExtractionCandidateByIdQuery(Guid CandidateId, Guid UserId) : IRequest<ExtractionCandidateDto?>;
