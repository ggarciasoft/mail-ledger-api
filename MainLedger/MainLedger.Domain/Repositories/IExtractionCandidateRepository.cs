using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;

namespace MainLedger.Domain.Repositories;

/// <summary>
/// Repository interface for ExtractionCandidate entity.
/// </summary>
public interface IExtractionCandidateRepository
{
    Task<ExtractionCandidate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(List<ExtractionCandidate> Candidates, int TotalCount)> GetPagedAsync(
        Guid userId,
        RecordStatus? status,
        int page,
        int pageSize,
        string sortBy,
        string sortOrder,
        CancellationToken cancellationToken = default
    );
    Task AddAsync(ExtractionCandidate candidate, CancellationToken cancellationToken = default);
    void Update(ExtractionCandidate candidate);
    Task<bool> HasCandidatesForEmailAsync(
        Guid emailId,
        CancellationToken cancellationToken = default
    );
}
