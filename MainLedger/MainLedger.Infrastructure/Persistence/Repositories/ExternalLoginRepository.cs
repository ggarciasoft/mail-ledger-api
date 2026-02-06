using MainLedger.Domain.Entities;
using MainLedger.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MainLedger.Infrastructure.Persistence.Repositories;

public class ExternalLoginRepository : IExternalLoginRepository
{
    private readonly MailLedgerDbContext _context;

    public ExternalLoginRepository(MailLedgerDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<ExternalLogin?> GetByProviderAndUserIdAsync(
        string provider,
        string providerUserId,
        CancellationToken cancellationToken = default
    )
    {
        return await _context.ExternalLogins
            .FirstOrDefaultAsync(
                el => el.Provider == provider && el.ProviderUserId == providerUserId,
                cancellationToken
            );
    }

    public async Task<ExternalLogin?> GetByUserIdAndProviderAsync(
        Guid userId,
        string provider,
        CancellationToken cancellationToken = default
    )
    {
        return await _context.ExternalLogins
            .FirstOrDefaultAsync(
                el => el.UserId == userId && el.Provider == provider,
                cancellationToken
            );
    }

    public async Task<List<ExternalLogin>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        return await _context.ExternalLogins
            .Where(el => el.UserId == userId)
            .OrderByDescending(el => el.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ExternalLogin externalLogin, CancellationToken cancellationToken = default)
    {
        await _context.ExternalLogins.AddAsync(externalLogin, cancellationToken);
    }

    public Task UpdateAsync(ExternalLogin externalLogin, CancellationToken cancellationToken = default)
    {
        _context.ExternalLogins.Update(externalLogin);
        return Task.CompletedTask;
    }
}
