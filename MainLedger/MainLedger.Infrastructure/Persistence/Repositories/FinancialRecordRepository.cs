using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using MainLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MainLedger.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for FinancialRecord entity.
/// </summary>
public class FinancialRecordRepository : IFinancialRecordRepository
{
    private readonly MailLedgerDbContext _context;

    public FinancialRecordRepository(MailLedgerDbContext context)
    {
        _context = context;
    }

    public async Task<FinancialRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.FinancialRecords
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public async Task<List<FinancialRecord>> GetByUserIdAsync(
        Guid userId,
        RecordStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.FinancialRecords
            .Where(f => f.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(f => f.Status == status.Value);
        }

        return await query
            .OrderByDescending(f => f.TransactionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<FinancialRecord>> GetConfirmedByUserIdAsync(
        Guid userId,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.FinancialRecords
            .Where(f => f.UserId == userId && f.Status == RecordStatus.Confirmed);

        if (from.HasValue)
        {
            query = query.Where(f => f.TransactionDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(f => f.TransactionDate <= to.Value);
        }

        return await query
            .OrderByDescending(f => f.TransactionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(FinancialRecord record, CancellationToken cancellationToken = default)
    {
        await _context.FinancialRecords.AddAsync(record, cancellationToken);
    }

    public void Update(FinancialRecord record)
    {
        _context.FinancialRecords.Update(record);
    }
}
