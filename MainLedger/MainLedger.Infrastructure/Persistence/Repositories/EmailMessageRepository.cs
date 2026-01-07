using MainLedger.Domain.Entities;
using MainLedger.Domain.Repositories;
using MainLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MainLedger.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for EmailMessage entity.
/// </summary>
public class EmailMessageRepository : IEmailMessageRepository
{
    private readonly MailLedgerDbContext _context;

    public EmailMessageRepository(MailLedgerDbContext context)
    {
        _context = context;
    }

    public async Task<EmailMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.EmailMessages
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<EmailMessage?> GetByMessageIdAsync(string messageId, CancellationToken cancellationToken = default)
    {
        return await _context.EmailMessages
            .FirstOrDefaultAsync(e => e.MessageId == messageId, cancellationToken);
    }

    public async Task<bool> ExistsByContentHashAsync(string contentHash, CancellationToken cancellationToken = default)
    {
        return await _context.EmailMessages
            .AnyAsync(e => e.ContentHash == contentHash, cancellationToken);
    }

    public async Task<List<EmailMessage>> GetUnprocessedAsync(Guid userId, int limit, CancellationToken cancellationToken = default)
    {
        return await _context.EmailMessages
            .Where(e => e.UserId == userId && !e.IsProcessed)
            .OrderBy(e => e.ReceivedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        await _context.EmailMessages.AddAsync(message, cancellationToken);
    }

    public void Update(EmailMessage message)
    {
        _context.EmailMessages.Update(message);
    }
}
