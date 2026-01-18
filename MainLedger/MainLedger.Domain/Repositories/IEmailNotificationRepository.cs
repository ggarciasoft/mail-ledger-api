using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;

namespace MainLedger.Domain.Repositories;

public interface IEmailNotificationRepository
{
    Task AddAsync(EmailNotification notification, CancellationToken cancellationToken = default);
    Task<List<EmailNotification>> GetPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default
    );
    Task UpdateAsync(EmailNotification notification, CancellationToken cancellationToken = default);
}
