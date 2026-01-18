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

    public async Task<FinancialRecord?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        return await _context.FinancialRecords.FirstOrDefaultAsync(
            f => f.Id == id,
            cancellationToken
        );
    }

    public async Task<List<FinancialRecord>> GetByUserIdAsync(
        Guid userId,
        RecordStatus? status = null,
        CancellationToken cancellationToken = default
    )
    {
        var query = _context.FinancialRecords.Where(f => f.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(f => f.Status == status.Value);
        }

        return await query.OrderByDescending(f => f.TransactionDate).ToListAsync(cancellationToken);
    }

    public async Task<List<FinancialRecord>> GetConfirmedByUserIdAsync(
        Guid userId,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default
    )
    {
        var query = _context.FinancialRecords.Where(f =>
            f.UserId == userId && f.Status == RecordStatus.Confirmed
        );

        if (from.HasValue)
        {
            query = query.Where(f => f.TransactionDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(f => f.TransactionDate <= to.Value);
        }

        return await query.OrderByDescending(f => f.TransactionDate).ToListAsync(cancellationToken);
    }

    public async Task<(List<FinancialRecord> Records, int TotalCount)> GetPagedAsync(
        Guid userId,
        DateTime? startDate,
        DateTime? endDate,
        decimal? minAmount,
        decimal? maxAmount,
        string? merchant,
        string? currency,
        string? sourceBank,
        int page,
        int pageSize,
        string sortBy,
        string sortOrder,
        CancellationToken cancellationToken = default
    )
    {
        // Only show confirmed records
        var query = _context.FinancialRecords.Where(f =>
            f.UserId == userId && f.Status == RecordStatus.Confirmed
        );

        // Apply filters
        if (startDate.HasValue)
        {
            query = query.Where(f => f.TransactionDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(f => f.TransactionDate <= endDate.Value);
        }

        if (minAmount.HasValue)
        {
            query = query.Where(f => f.Amount.Amount >= minAmount.Value);
        }

        if (maxAmount.HasValue)
        {
            query = query.Where(f => f.Amount.Amount <= maxAmount.Value);
        }

        if (!string.IsNullOrWhiteSpace(merchant))
        {
            query = query.Where(f => f.Merchant != null && f.Merchant.Contains(merchant));
        }

        if (!string.IsNullOrWhiteSpace(currency))
        {
            query = query.Where(f => f.Amount.Currency.ToString() == currency);
        }

        if (!string.IsNullOrWhiteSpace(sourceBank))
        {
            query = query.Where(f =>
                f.SourceBank != null && f.SourceBank.Name.Contains(sourceBank)
            );
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = sortBy.ToLowerInvariant() switch
        {
            "amount" => sortOrder.ToLowerInvariant() == "asc"
                ? query.OrderBy(f => f.Amount.Amount)
                : query.OrderByDescending(f => f.Amount.Amount),
            "merchant" => sortOrder.ToLowerInvariant() == "asc"
                ? query.OrderBy(f => f.Merchant)
                : query.OrderByDescending(f => f.Merchant),
            "type" => sortOrder.ToLowerInvariant() == "asc"
                ? query.OrderBy(f => f.Type)
                : query.OrderByDescending(f => f.Type),
            _ => sortOrder.ToLowerInvariant() == "asc"
                ? query.OrderBy(f => f.TransactionDate)
                : query.OrderByDescending(f => f.TransactionDate),
        };

        // Apply pagination
        var records = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (records, totalCount);
    }

    public async Task<List<FinancialRecord>> GetFilteredAsync(
        Guid userId,
        DateTime? startDate,
        DateTime? endDate,
        decimal? minAmount,
        decimal? maxAmount,
        string? merchant,
        string? currency,
        string? sourceBank,
        string sortBy,
        string sortOrder,
        CancellationToken cancellationToken = default
    )
    {
        // Only show confirmed records
        var query = _context.FinancialRecords.Where(f =>
            f.UserId == userId && f.Status == RecordStatus.Confirmed
        );

        // Apply filters
        if (startDate.HasValue)
        {
            query = query.Where(f => f.TransactionDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(f => f.TransactionDate <= endDate.Value);
        }

        if (minAmount.HasValue)
        {
            query = query.Where(f => f.Amount.Amount >= minAmount.Value);
        }

        if (maxAmount.HasValue)
        {
            query = query.Where(f => f.Amount.Amount <= maxAmount.Value);
        }

        if (!string.IsNullOrWhiteSpace(merchant))
        {
            query = query.Where(f => f.Merchant != null && f.Merchant.Contains(merchant));
        }

        if (!string.IsNullOrWhiteSpace(currency))
        {
            query = query.Where(f => f.Amount.Currency.ToString() == currency);
        }

        if (!string.IsNullOrWhiteSpace(sourceBank))
        {
            query = query.Where(f =>
                f.SourceBank != null && f.SourceBank.Name.Contains(sourceBank)
            );
        }

        // Apply sorting
        query = sortBy.ToLowerInvariant() switch
        {
            "amount" => sortOrder.ToLowerInvariant() == "asc"
                ? query.OrderBy(f => f.Amount.Amount)
                : query.OrderByDescending(f => f.Amount.Amount),
            "merchant" => sortOrder.ToLowerInvariant() == "asc"
                ? query.OrderBy(f => f.Merchant)
                : query.OrderByDescending(f => f.Merchant),
            "type" => sortOrder.ToLowerInvariant() == "asc"
                ? query.OrderBy(f => f.Type)
                : query.OrderByDescending(f => f.Type),
            _ => sortOrder.ToLowerInvariant() == "asc"
                ? query.OrderBy(f => f.TransactionDate)
                : query.OrderByDescending(f => f.TransactionDate),
        };

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<Domain.Models.FinancialRecordStatistics> GetStatisticsAsync(
        Guid userId,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken = default
    )
    {
        var query = _context.FinancialRecords.Where(f =>
            f.UserId == userId && f.Status == RecordStatus.Confirmed
        );

        if (startDate.HasValue)
        {
            query = query.Where(f => f.TransactionDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(f => f.TransactionDate <= endDate.Value);
        }

        var records = await query.ToListAsync(cancellationToken);

        var totalAmount = records.Sum(r => r.Amount.Amount);
        var averageAmount = records.Any() ? records.Average(r => r.Amount.Amount) : 0;

        // Group by type
        var byType = records
            .GroupBy(r => r.Type.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        // Group by bank
        var byBank = records
            .Where(r => r.SourceBank != null)
            .GroupBy(r => r.SourceBank!.Name)
            .ToDictionary(g => g.Key, g => g.Count());

        // Monthly trend
        var monthlyTrend = records
            .GroupBy(r => new { r.TransactionDate.Year, r.TransactionDate.Month })
            .Select(g => new Domain.Models.MonthlyTrend
            {
                Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                Count = g.Count(),
                Total = g.Sum(r => r.Amount.Amount),
            })
            .OrderBy(t => t.Month)
            .ToList();

        return new Domain.Models.FinancialRecordStatistics
        {
            TotalRecords = records.Count,
            TotalAmount = totalAmount,
            AverageAmount = averageAmount,
            ByType = byType,
            ByBank = byBank,
            MonthlyTrend = monthlyTrend,
        };
    }

    public async Task AddAsync(
        FinancialRecord record,
        CancellationToken cancellationToken = default
    )
    {
        await _context.FinancialRecords.AddAsync(record, cancellationToken);
    }

    public void Update(FinancialRecord record)
    {
        _context.FinancialRecords.Update(record);
    }
}
