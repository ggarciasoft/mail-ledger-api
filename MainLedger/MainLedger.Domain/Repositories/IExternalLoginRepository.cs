using MainLedger.Domain.Entities;

namespace MainLedger.Domain.Repositories;

public interface IExternalLoginRepository
{
    Task<ExternalLogin?> GetByProviderAndUserIdAsync(string provider, string providerUserId, CancellationToken cancellationToken = default);
    Task<ExternalLogin?> GetByUserIdAndProviderAsync(Guid userId, string provider, CancellationToken cancellationToken = default);
    Task<List<ExternalLogin>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(ExternalLogin externalLogin, CancellationToken cancellationToken = default);
    Task UpdateAsync(ExternalLogin externalLogin, CancellationToken cancellationToken = default);
}
