using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;

namespace MainLedger.Domain.Repositories;

/// <summary>
/// Repository interface for FinancialRecord entity.
/// </summary>
public interface IFinancialRecordRepository
{
    Task<FinancialRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<FinancialRecord>> GetByUserIdAsync(
        Guid userId,
        RecordStatus? status = null,
        CancellationToken cancellationToken = default
    );
    Task<List<FinancialRecord>> GetConfirmedByUserIdAsync(
        Guid userId,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default
    );
    Task<(List<FinancialRecord> Records, int TotalCount)> GetPagedAsync(
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
    );
    Task<Models.FinancialRecordStatistics> GetStatisticsAsync(
        Guid userId,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken = default
    );
    Task<List<FinancialRecord>> GetFilteredAsync(
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
    );
    Task AddAsync(FinancialRecord record, CancellationToken cancellationToken = default);
    void Update(FinancialRecord record);
}
