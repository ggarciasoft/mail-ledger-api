using MainLedger.Domain.Entities;
using MainLedger.Domain.Repositories;
using MainLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MainLedger.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for EmailMessage entity.
/// </summary>
public class EmailMessageRepository : IEmailMessageRepository
{
    private readonly MailLedgerDbContext _context;

    public EmailMessageRepository(MailLedgerDbContext context)
    {
        _context = context;
    }

    public async Task<EmailMessage?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        return await _context.EmailMessages.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<EmailMessage?> GetByMessageIdAsync(
        string messageId,
        CancellationToken cancellationToken = default
    )
    {
        return await _context.EmailMessages.FirstOrDefaultAsync(
            e => e.MessageId == messageId,
            cancellationToken
        );
    }

    public async Task<bool> ExistsByContentHashAsync(
        string contentHash,
        CancellationToken cancellationToken = default
    )
    {
        return await _context.EmailMessages.AnyAsync(
            e => e.ContentHash == contentHash,
            cancellationToken
        );
    }

    public async Task<List<EmailMessage>> GetUnprocessedAsync(
        Guid userId,
        int limit,
        CancellationToken cancellationToken = default
    )
    {
        return await _context
            .EmailMessages.Where(e => e.UserId == userId && !e.IsProcessed)
            .OrderBy(e => e.ReceivedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<EmailMessage>> GetByProcessingStatusAsync(
        Guid userId,
        Domain.Enums.EmailProcessingStatus status,
        int limit,
        CancellationToken cancellationToken = default
    )
    {
        return await _context
            .EmailMessages.Where(e => e.UserId == userId && e.ProcessingStatus == status)
            .OrderBy(e => e.ReceivedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<EmailMessage>> GetClassifiedFinancialEmailsAsync(
        Guid userId,
        int limit,
        CancellationToken cancellationToken = default
    )
    {
        return await _context
            .EmailMessages.Where(e =>
                e.UserId == userId
                && e.ProcessingStatus == Domain.Enums.EmailProcessingStatus.Classified
                && e.IsFinancial == true
            )
            .OrderBy(e => e.ReceivedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<EmailMessage>> GetPendingClassificationAsync(
        Guid userId,
        int limit,
        CancellationToken cancellationToken = default
    )
    {
        return await _context
            .EmailMessages.Where(e =>
                e.UserId == userId
                && e.ProcessingStatus == Domain.Enums.EmailProcessingStatus.Pending
                && e.IsFinancial == null
            )
            .OrderBy(e => e.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<EmailMessage>> GetPendingExtractionAsync(
        Guid userId,
        int limit,
        CancellationToken cancellationToken = default
    )
    {
        return await _context
            .EmailMessages.Where(e =>
                e.UserId == userId
                && e.ProcessingStatus == Domain.Enums.EmailProcessingStatus.Classified
                && e.IsFinancial == true
            )
            .OrderBy(e => e.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<(List<EmailMessage> Emails, int TotalCount)> GetPagedAsync(
        Guid userId,
        Domain.Enums.EmailProcessingStatus? status,
        bool? isFinancial,
        int page,
        int pageSize,
        string sortBy,
        string sortOrder,
        CancellationToken cancellationToken = default
    )
    {
        var query = _context.EmailMessages.Where(e => e.UserId == userId);

        // Apply filters
        if (status.HasValue)
        {
            query = query.Where(e => e.ProcessingStatus == status.Value);
        }

        if (isFinancial.HasValue)
        {
            query = query.Where(e => e.IsFinancial == isFinancial.Value);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = sortBy.ToLowerInvariant() switch
        {
            "subject" => sortOrder.ToLowerInvariant() == "asc"
                ? query.OrderBy(e => e.Subject)
                : query.OrderByDescending(e => e.Subject),
            "from" => sortOrder.ToLowerInvariant() == "asc"
                ? query.OrderBy(e => e.From.Value)
                : query.OrderByDescending(e => e.From.Value),
            "processingStatus" => sortOrder.ToLowerInvariant() == "asc"
                ? query.OrderBy(e => e.ProcessingStatus)
                : query.OrderByDescending(e => e.ProcessingStatus),
            _ => sortOrder.ToLowerInvariant() == "asc"
                ? query.OrderBy(e => e.ReceivedAt)
                : query.OrderByDescending(e => e.ReceivedAt),
        };

        // Apply pagination
        var emails = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (emails, totalCount);
    }

    public async Task<Domain.Models.EmailStatistics> GetStatisticsAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var emails = await _context
            .EmailMessages.Where(e => e.UserId == userId)
            .ToListAsync(cancellationToken);

        return new Domain.Models.EmailStatistics
        {
            TotalEmails = emails.Count,
            Pending = emails.Count(e =>
                e.ProcessingStatus == Domain.Enums.EmailProcessingStatus.Pending
            ),
            Classified = emails.Count(e =>
                e.ProcessingStatus == Domain.Enums.EmailProcessingStatus.Classified
            ),
            Extracted = emails.Count(e =>
                e.ProcessingStatus == Domain.Enums.EmailProcessingStatus.Extracted
            ),
            Failed = emails.Count(e =>
                e.ProcessingStatus == Domain.Enums.EmailProcessingStatus.Failed
            ),
            FinancialEmails = emails.Count(e => e.IsFinancial == true),
            NonFinancialEmails = emails.Count(e => e.IsFinancial == false),
        };
    }

    public async Task<int> CountByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        return await _context
            .EmailMessages.Where(e => e.UserId == userId)
            .CountAsync(cancellationToken);
    }

    public async Task<List<Domain.Models.SyncHistoryItem>> GetSyncHistoryAsync(
        Guid userId,
        int limit,
        CancellationToken cancellationToken = default
    )
    {
        // Get sync history from the GmailSyncHistory table
        var syncHistory = await _context
            .GmailSyncHistories.Where(s => s.UserId == userId && s.SyncCompletedAt != null)
            .OrderByDescending(s => s.SyncStartedAt)
            .Take(limit)
            .Select(s => new Domain.Models.SyncHistoryItem
            {
                SyncedAt = s.SyncStartedAt,
                EmailCount = s.EmailsProcessed,
            })
            .ToListAsync(cancellationToken);

        return syncHistory;
    }

    public async Task AddAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        await _context.EmailMessages.AddAsync(message, cancellationToken);
    }

    public void Update(EmailMessage message)
    {
        _context.EmailMessages.Update(message);
    }
}
