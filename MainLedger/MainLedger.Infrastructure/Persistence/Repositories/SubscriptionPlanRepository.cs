using MainLedger.Domain.Entities;
using MainLedger.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MainLedger.Infrastructure.Persistence.Repositories;

public class SubscriptionPlanRepository : ISubscriptionPlanRepository
{
    private readonly MailLedgerDbContext _context;

    public SubscriptionPlanRepository(MailLedgerDbContext context)
    {
        _context = context;
    }

    public async Task<SubscriptionPlan?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        return await _context.SubscriptionPlans.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<List<SubscriptionPlan>> GetAllActiveAsync(
        CancellationToken cancellationToken = default
    )
    {
        return await _context
            .SubscriptionPlans.Where(p => p.IsActive)
            .OrderBy(p => p.MonthlyPrice)
            .ToListAsync(cancellationToken);
    }

    public async Task<SubscriptionPlan?> GetByNameAsync(
        string name,
        CancellationToken cancellationToken = default
    )
    {
        return await _context.SubscriptionPlans.FirstOrDefaultAsync(
            p => p.Name == name,
            cancellationToken
        );
    }

    public void Add(SubscriptionPlan plan)
    {
        _context.SubscriptionPlans.Add(plan);
    }

    public void Update(SubscriptionPlan plan)
    {
        _context.SubscriptionPlans.Update(plan);
    }
}
