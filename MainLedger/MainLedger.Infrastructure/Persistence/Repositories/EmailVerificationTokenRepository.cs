using MainLedger.Domain.Entities;
using MainLedger.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MainLedger.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for EmailVerificationToken entity.
/// </summary>
public class EmailVerificationTokenRepository : IEmailVerificationTokenRepository
{
    private readonly MailLedgerDbContext _context;

    public EmailVerificationTokenRepository(MailLedgerDbContext context)
    {
        _context = context;
    }

    public async Task<EmailVerificationToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.EmailVerificationTokens
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<EmailVerificationToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _context.EmailVerificationTokens
            .FirstOrDefaultAsync(e => e.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<List<EmailVerificationToken>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.EmailVerificationTokens
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(EmailVerificationToken token, CancellationToken cancellationToken = default)
    {
        await _context.EmailVerificationTokens.AddAsync(token, cancellationToken);
    }

    public async Task UpdateAsync(EmailVerificationToken token, CancellationToken cancellationToken = default)
    {
        _context.EmailVerificationTokens.Update(token);
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.EmailVerificationTokens
            .AnyAsync(e => e.Id == id, cancellationToken);
    }
}
