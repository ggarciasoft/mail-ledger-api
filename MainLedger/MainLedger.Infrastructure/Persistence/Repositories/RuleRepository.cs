using MainLedger.Domain.Entities;
using MainLedger.Domain.Repositories;
using MainLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MainLedger.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Rule entity.
/// </summary>
public class RuleRepository : IRuleRepository
{
    private readonly MailLedgerDbContext _context;

    public RuleRepository(MailLedgerDbContext context)
    {
        _context = context;
    }

    public async Task<List<Rule>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Rules
            .Where(r => r.UserId == userId && r.IsActive)
            .OrderBy(r => r.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task<Rule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Rules
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task AddAsync(Rule rule, CancellationToken cancellationToken = default)
    {
        await _context.Rules.AddAsync(rule, cancellationToken);
    }

    public void Update(Rule rule)
    {
        _context.Rules.Update(rule);
    }
}
