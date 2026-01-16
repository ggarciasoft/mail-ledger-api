using MainLedger.Domain.Entities;

namespace MainLedger.Domain.Repositories;

/// <summary>
/// Repository interface for subscription plan operations.
/// </summary>
public interface ISubscriptionPlanRepository
{
    Task<SubscriptionPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<SubscriptionPlan>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<SubscriptionPlan?> GetByNameAsync(
        string name,
        CancellationToken cancellationToken = default
    );
    void Add(SubscriptionPlan plan);
    void Update(SubscriptionPlan plan);
}
