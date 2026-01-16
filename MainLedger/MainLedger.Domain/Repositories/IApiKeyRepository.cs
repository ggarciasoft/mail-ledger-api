using MainLedger.Domain.Entities;

namespace MainLedger.Domain.Repositories;

/// <summary>
/// Repository interface for ApiKey entity.
/// </summary>
public interface IApiKeyRepository
{
    Task<ApiKey?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiKey?> GetByKeyHashAsync(string keyHash, CancellationToken cancellationToken = default);
    Task<List<ApiKey>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<ApiKey>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task AddAsync(ApiKey apiKey, CancellationToken cancellationToken = default);
    Task UpdateAsync(ApiKey apiKey, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
