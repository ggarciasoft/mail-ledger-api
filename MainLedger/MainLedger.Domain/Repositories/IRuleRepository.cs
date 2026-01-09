using MainLedger.Domain.Entities;

namespace MainLedger.Domain.Repositories;

/// <summary>
/// Repository interface for Rule entity.
/// </summary>
public interface IRuleRepository
{
    Task<List<Rule>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<Rule>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Rule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Rule rule, CancellationToken cancellationToken = default);
    void Update(Rule rule);
}
