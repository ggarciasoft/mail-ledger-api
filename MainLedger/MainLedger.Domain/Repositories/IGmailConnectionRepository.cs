using MainLedger.Domain.Entities;

namespace MainLedger.Domain.Repositories;

/// <summary>
/// Repository interface for GmailConnection entity.
/// </summary>
public interface IGmailConnectionRepository
{
    Task<GmailConnection?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<GmailConnection?> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(GmailConnection connection, CancellationToken cancellationToken = default);
    void Update(GmailConnection connection);
}
