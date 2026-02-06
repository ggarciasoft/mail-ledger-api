using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MainLedger.Infrastructure.Persistence.Repositories;

public class UserSubscriptionRepository : IUserSubscriptionRepository
{
    private readonly MailLedgerDbContext _context;

    public UserSubscriptionRepository(MailLedgerDbContext context)
    {
        _context = context;
    }

    public async Task<UserSubscription?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        return await _context
            .UserSubscriptions.Include(s => s.SubscriptionPlan)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<UserSubscription?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        return await _context
            .UserSubscriptions.Include(s => s.SubscriptionPlan)
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
    }

    public async Task<UserSubscription?> GetByStripeSubscriptionIdAsync(
        string stripeSubscriptionId,
        CancellationToken cancellationToken = default
    )
    {
        return await _context
            .UserSubscriptions.Include(s => s.SubscriptionPlan)
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscriptionId, cancellationToken);
    }

    public async Task<List<UserSubscription>> GetExpiredSubscriptionsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var now = DateTime.UtcNow;
        return await _context
            .UserSubscriptions.Include(s => s.SubscriptionPlan)
            .Where(s =>
                s.Status == SubscriptionStatus.Active
                && s.EndDate.HasValue
                && s.EndDate.Value <= now
            )
            .ToListAsync(cancellationToken);
    }

    public void Add(UserSubscription subscription)
    {
        _context.UserSubscriptions.Add(subscription);
    }

    public async Task AddAsync(UserSubscription subscription, CancellationToken cancellationToken = default)
    {
        await _context.UserSubscriptions.AddAsync(subscription, cancellationToken);
    }

    public void Update(UserSubscription subscription)
    {
        _context.UserSubscriptions.Update(subscription);
    }
}
