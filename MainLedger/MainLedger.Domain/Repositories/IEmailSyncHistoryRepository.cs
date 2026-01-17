using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;

namespace MainLedger.Domain.Repositories;

public interface IEmailSyncHistoryRepository
{
    Task<EmailSyncHistory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EmailSyncHistory?> GetLatestByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );
    Task<List<EmailSyncHistory>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );
    Task<List<EmailSyncHistory>> GetByUserAndProviderAsync(
        Guid userId,
        EmailProvider provider,
        CancellationToken cancellationToken = default
    );
    Task AddAsync(EmailSyncHistory syncHistory, CancellationToken cancellationToken = default);
    Task UpdateAsync(EmailSyncHistory syncHistory, CancellationToken cancellationToken = default);
}
