using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MainLedger.Infrastructure.Persistence.Repositories;

public class EmailConnectionRepository : IEmailConnectionRepository
{
    private readonly MailLedgerDbContext _context;

    public EmailConnectionRepository(MailLedgerDbContext context)
    {
        _context = context;
    }

    public async Task<EmailConnection?> GetByIdAsync(Guid id)
    {
        return await _context
            .EmailConnections.Include(ec => ec.User)
            .FirstOrDefaultAsync(ec => ec.Id == id);
    }

    public async Task<EmailConnection?> GetByUserAndProviderAsync(
        Guid userId,
        EmailProvider provider
    )
    {
        return await _context
            .EmailConnections.Include(ec => ec.User)
            .FirstOrDefaultAsync(ec => ec.UserId == userId && ec.Provider == provider);
    }

    public async Task<EmailConnection?> GetByEmailAsync(string email)
    {
        return await _context
            .EmailConnections.Include(ec => ec.User)
            .FirstOrDefaultAsync(ec => ec.Email == email && ec.IsActive);
    }

    public async Task<List<EmailConnection>> GetByUserIdAsync(Guid userId)
    {
        return await _context
            .EmailConnections.Include(ec => ec.User)
            .Where(ec => ec.UserId == userId)
            .OrderBy(ec => ec.Provider)
            .ToListAsync();
    }

    public async Task<int> CountByUserAndProviderAsync(
        Guid userId,
        EmailProvider provider,
        CancellationToken cancellationToken = default
    )
    {
        return await _context.EmailConnections.CountAsync(
            ec => ec.UserId == userId && ec.Provider == provider && ec.IsActive,
            cancellationToken
        );
    }

    public async Task<EmailConnection> AddAsync(EmailConnection connection)
    {
        await _context.EmailConnections.AddAsync(connection);
        await _context.SaveChangesAsync();
        return connection;
    }

    public async Task UpdateAsync(EmailConnection connection)
    {
        _context.EmailConnections.Update(connection);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var connection = await GetByIdAsync(id);
        if (connection != null)
        {
            _context.EmailConnections.Remove(connection);
            await _context.SaveChangesAsync();
        }
    }
}
