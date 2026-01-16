using MainLedger.Domain.Entities;
using MainLedger.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MainLedger.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for ApiKey entity.
/// </summary>
public class ApiKeyRepository : IApiKeyRepository
{
    private readonly MailLedgerDbContext _context;

    public ApiKeyRepository(MailLedgerDbContext context)
    {
        _context = context;
    }

    public async Task<ApiKey?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ApiKeys.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<ApiKey?> GetByKeyHashAsync(
        string keyHash,
        CancellationToken cancellationToken = default
    )
    {
        return await _context.ApiKeys.FirstOrDefaultAsync(
            a => a.KeyHash == keyHash,
            cancellationToken
        );
    }

    public async Task<List<ApiKey>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        return await _context
            .ApiKeys.Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ApiKey>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ApiKeys.Where(a => a.IsActive).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ApiKey apiKey, CancellationToken cancellationToken = default)
    {
        await _context.ApiKeys.AddAsync(apiKey, cancellationToken);
    }

    public async Task UpdateAsync(ApiKey apiKey, CancellationToken cancellationToken = default)
    {
        _context.ApiKeys.Update(apiKey);
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ApiKeys.AnyAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<int> CountByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        return await _context.ApiKeys.CountAsync(
            a => a.UserId == userId && !a.IsRevoked,
            cancellationToken
        );
    }
}
