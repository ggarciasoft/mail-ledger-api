using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MainLedger.Infrastructure.Persistence.Repositories;

public class EmailSyncHistoryRepository : IEmailSyncHistoryRepository
{
    private readonly MailLedgerDbContext _context;

    public EmailSyncHistoryRepository(MailLedgerDbContext context)
    {
        _context = context;
    }

    public async Task<EmailSyncHistory?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        return await _context
            .EmailSyncHistories.Include(sh => sh.User)
            .FirstOrDefaultAsync(sh => sh.Id == id, cancellationToken);
    }

    public async Task<EmailSyncHistory?> GetLatestByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        return await _context
            .EmailSyncHistories.Include(sh => sh.User)
            .Where(sh => sh.UserId == userId)
            .OrderByDescending(sh => sh.SyncStartedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<EmailSyncHistory>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        return await _context
            .EmailSyncHistories.Include(sh => sh.User)
            .Where(sh => sh.UserId == userId)
            .OrderByDescending(sh => sh.SyncStartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<EmailSyncHistory>> GetByUserAndProviderAsync(
        Guid userId,
        EmailProvider provider,
        CancellationToken cancellationToken = default
    )
    {
        return await _context
            .EmailSyncHistories.Include(sh => sh.User)
            .Where(sh => sh.UserId == userId && sh.Provider == provider)
            .OrderByDescending(sh => sh.SyncStartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        EmailSyncHistory syncHistory,
        CancellationToken cancellationToken = default
    )
    {
        await _context.EmailSyncHistories.AddAsync(syncHistory, cancellationToken);
    }

    public async Task UpdateAsync(
        EmailSyncHistory syncHistory,
        CancellationToken cancellationToken = default
    )
    {
        _context.EmailSyncHistories.Update(syncHistory);
        await Task.CompletedTask;
    }
}
