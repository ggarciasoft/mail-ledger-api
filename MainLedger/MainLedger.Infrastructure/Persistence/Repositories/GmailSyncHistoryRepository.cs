using MainLedger.Domain.Entities;
using MainLedger.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MainLedger.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for GmailSyncHistory entity.
/// </summary>
public class GmailSyncHistoryRepository : IGmailSyncHistoryRepository
{
    private readonly MailLedgerDbContext _context;

    public GmailSyncHistoryRepository(MailLedgerDbContext context)
    {
        _context = context;
    }

    public async Task<List<GmailSyncHistory>> GetByUserIdAsync(
        Guid userId,
        int limit,
        CancellationToken cancellationToken = default
    )
    {
        return await _context
            .GmailSyncHistories.Where(s => s.UserId == userId)
            .OrderByDescending(s => s.SyncStartedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<GmailSyncHistory?> GetLatestByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        return await _context
            .GmailSyncHistories.Where(s => s.UserId == userId)
            .OrderByDescending(s => s.SyncStartedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(
        GmailSyncHistory syncHistory,
        CancellationToken cancellationToken = default
    )
    {
        await _context.GmailSyncHistories.AddAsync(syncHistory, cancellationToken);
    }

    public async Task UpdateAsync(
        GmailSyncHistory syncHistory,
        CancellationToken cancellationToken = default
    )
    {
        _context.GmailSyncHistories.Update(syncHistory);
        await Task.CompletedTask;
    }
}
