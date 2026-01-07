using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;

namespace MainLedger.Domain.Repositories;

/// <summary>
/// Repository interface for FinancialRecord entity.
/// </summary>
public interface IFinancialRecordRepository
{
    Task<FinancialRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<FinancialRecord>> GetByUserIdAsync(Guid userId, RecordStatus? status = null, CancellationToken cancellationToken = default);
    Task<List<FinancialRecord>> GetConfirmedByUserIdAsync(Guid userId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    Task AddAsync(FinancialRecord record, CancellationToken cancellationToken = default);
    void Update(FinancialRecord record);
}
