using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MainLedger.Infrastructure.Persistence.Repositories;

public class EmailNotificationRepository : IEmailNotificationRepository
{
    private readonly MailLedgerDbContext _context;

    public EmailNotificationRepository(MailLedgerDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        EmailNotification notification,
        CancellationToken cancellationToken = default
    )
    {
        await _context.EmailNotifications.AddAsync(notification, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<EmailNotification>> GetPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default
    )
    {
        return await _context
            .EmailNotifications.Where(e => e.Status == EmailStatus.Pending)
            .OrderBy(e => e.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        EmailNotification notification,
        CancellationToken cancellationToken = default
    )
    {
        _context.EmailNotifications.Update(notification);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
