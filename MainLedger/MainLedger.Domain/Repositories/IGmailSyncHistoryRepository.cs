using MainLedger.Domain.Entities;

namespace MainLedger.Domain.Repositories;

/// <summary>
/// Repository interface for GmailSyncHistory entity.
/// </summary>
public interface IGmailSyncHistoryRepository
{
    Task<List<GmailSyncHistory>> GetByUserIdAsync(
        Guid userId,
        int limit,
        CancellationToken cancellationToken = default
    );
    Task<GmailSyncHistory?> GetLatestByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );
    Task AddAsync(GmailSyncHistory syncHistory, CancellationToken cancellationToken = default);
    Task UpdateAsync(GmailSyncHistory syncHistory, CancellationToken cancellationToken = default);
}
