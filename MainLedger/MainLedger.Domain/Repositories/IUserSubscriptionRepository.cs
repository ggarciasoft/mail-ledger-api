using MainLedger.Domain.Entities;

namespace MainLedger.Domain.Repositories;

/// <summary>
/// Repository interface for user subscription operations.
/// </summary>
public interface IUserSubscriptionRepository
{
    Task<UserSubscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserSubscription?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );
    Task<UserSubscription?> GetByStripeSubscriptionIdAsync(
        string stripeSubscriptionId,
        CancellationToken cancellationToken = default
    );
    Task<List<UserSubscription>> GetExpiredSubscriptionsAsync(
        CancellationToken cancellationToken = default
    );
    void Add(UserSubscription subscription);
    Task AddAsync(UserSubscription subscription, CancellationToken cancellationToken = default);
    void Update(UserSubscription subscription);
}
