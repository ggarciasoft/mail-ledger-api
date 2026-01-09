using MainLedger.Domain.Entities;

namespace MainLedger.Domain.Repositories;

/// <summary>
/// Repository interface for EmailMessage entity.
/// </summary>
public interface IEmailMessageRepository
{
    Task<EmailMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EmailMessage?> GetByMessageIdAsync(string messageId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByContentHashAsync(string contentHash, CancellationToken cancellationToken = default);
    Task<List<EmailMessage>> GetUnprocessedAsync(Guid userId, int limit, CancellationToken cancellationToken = default);
    Task<List<EmailMessage>> GetByProcessingStatusAsync(Guid userId, Enums.EmailProcessingStatus status, int limit, CancellationToken cancellationToken = default);
    Task<List<EmailMessage>> GetClassifiedFinancialEmailsAsync(Guid userId, int limit, CancellationToken cancellationToken = default);
    Task<(List<EmailMessage> Emails, int TotalCount)> GetPagedAsync(
        Guid userId,
        Enums.EmailProcessingStatus? status,
        bool? isFinancial,
        int page,
        int pageSize,
        string sortBy,
        string sortOrder,
        CancellationToken cancellationToken = default);
    Task<Models.EmailStatistics> GetStatisticsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<Models.SyncHistoryItem>> GetSyncHistoryAsync(Guid userId, int limit, CancellationToken cancellationToken = default);
    Task AddAsync(EmailMessage message, CancellationToken cancellationToken = default);
    void Update(EmailMessage message);
}
