using MainLedger.Domain.Entities;
using MainLedger.Domain.Repositories;
using MainLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MainLedger.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for GmailConnection entity.
/// </summary>
public class GmailConnectionRepository : IGmailConnectionRepository
{
    private readonly MailLedgerDbContext _context;

    public GmailConnectionRepository(MailLedgerDbContext context)
    {
        _context = context;
    }

    public async Task<GmailConnection?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        return await _context.GmailConnections.FirstOrDefaultAsync(
            g => g.UserId == userId,
            cancellationToken
        );
    }

    public async Task<GmailConnection?> GetActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        return await _context.GmailConnections.FirstOrDefaultAsync(
            g => g.UserId == userId && g.IsActive,
            cancellationToken
        );
    }

    public async Task AddAsync(
        GmailConnection connection,
        CancellationToken cancellationToken = default
    )
    {
        await _context.GmailConnections.AddAsync(connection, cancellationToken);
    }

    public void Update(GmailConnection connection)
    {
        _context.GmailConnections.Update(connection);
    }

    public async Task<int> CountByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        return await _context.GmailConnections.CountAsync(
            g => g.UserId == userId && g.IsActive,
            cancellationToken
        );
    }
}
