using MainLedger.Domain.Entities;
using MainLedger.Domain.Repositories;
using MainLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MainLedger.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for ExtractionVersion entity.
/// </summary>
public class ExtractionVersionRepository : IExtractionVersionRepository
{
    private readonly MailLedgerDbContext _context;

    public ExtractionVersionRepository(MailLedgerDbContext context)
    {
        _context = context;
    }

    public async Task<ExtractionVersion?> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ExtractionVersions
            .FirstOrDefaultAsync(e => e.IsActive, cancellationToken);
    }

    public async Task<ExtractionVersion?> GetByVersionAsync(string version, CancellationToken cancellationToken = default)
    {
        return await _context.ExtractionVersions
            .FirstOrDefaultAsync(e => e.Version == version, cancellationToken);
    }

    public async Task AddAsync(ExtractionVersion version, CancellationToken cancellationToken = default)
    {
        await _context.ExtractionVersions.AddAsync(version, cancellationToken);
    }

    public void Update(ExtractionVersion version)
    {
        _context.ExtractionVersions.Update(version);
    }
}
