using MainLedger.Domain.Entities;
using MainLedger.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MainLedger.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for PasswordResetToken entity.
/// </summary>
public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly MailLedgerDbContext _context;

    public PasswordResetTokenRepository(MailLedgerDbContext context)
    {
        _context = context;
    }

    public async Task<PasswordResetToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.PasswordResetTokens
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _context.PasswordResetTokens
            .FirstOrDefaultAsync(p => p.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<List<PasswordResetToken>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.PasswordResetTokens
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default)
    {
        await _context.PasswordResetTokens.AddAsync(token, cancellationToken);
    }

    public async Task UpdateAsync(PasswordResetToken token, CancellationToken cancellationToken = default)
    {
        _context.PasswordResetTokens.Update(token);
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.PasswordResetTokens
            .AnyAsync(p => p.Id == id, cancellationToken);
    }
}
