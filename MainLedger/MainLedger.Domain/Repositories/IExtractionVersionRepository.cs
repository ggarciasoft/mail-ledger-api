using MainLedger.Domain.Entities;

namespace MainLedger.Domain.Repositories;

/// <summary>
/// Repository interface for ExtractionVersion entity.
/// </summary>
public interface IExtractionVersionRepository
{
    Task<ExtractionVersion?> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<ExtractionVersion?> GetByVersionAsync(string version, CancellationToken cancellationToken = default);
    Task AddAsync(ExtractionVersion version, CancellationToken cancellationToken = default);
    void Update(ExtractionVersion version);
}
