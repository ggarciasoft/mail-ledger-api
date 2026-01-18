using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MainLedger.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for ExtractionCandidate entity.
/// </summary>
public class ExtractionCandidateRepository : IExtractionCandidateRepository
{
    private readonly MailLedgerDbContext _context;

    public ExtractionCandidateRepository(MailLedgerDbContext context)
    {
        _context = context;
    }

    public async Task<ExtractionCandidate?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        return await _context.ExtractionCandidates.FirstOrDefaultAsync(
            c => c.Id == id,
            cancellationToken
        );
    }

    public async Task<(List<ExtractionCandidate> Candidates, int TotalCount)> GetPagedAsync(
        Guid userId,
        RecordStatus? status,
        int page,
        int pageSize,
        string sortBy,
        string sortOrder,
        CancellationToken cancellationToken = default
    )
    {
        // Join with EmailMessage to filter by userId
        var query =
            from candidate in _context.ExtractionCandidates
            join email in _context.EmailMessages on candidate.EmailMessageId equals email.Id
            where email.UserId == userId
            select candidate;

        // Apply status filter
        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = sortBy.ToLowerInvariant() switch
        {
            "amount" => sortOrder.ToLowerInvariant() == "asc"
                ? query.OrderBy(c => c.Amount!.Amount)
                : query.OrderByDescending(c => c.Amount!.Amount),
            "merchant" => sortOrder.ToLowerInvariant() == "asc"
                ? query.OrderBy(c => c.Merchant)
                : query.OrderByDescending(c => c.Merchant),
            "transactiondate" => sortOrder.ToLowerInvariant() == "asc"
                ? query.OrderBy(c => c.TransactionDate)
                : query.OrderByDescending(c => c.TransactionDate),
            _ => sortOrder.ToLowerInvariant() == "asc"
                ? query.OrderBy(c => c.CreatedAt)
                : query.OrderByDescending(c => c.CreatedAt),
        };

        // Apply pagination
        var candidates = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (candidates, totalCount);
    }

    public async Task AddAsync(
        ExtractionCandidate candidate,
        CancellationToken cancellationToken = default
    )
    {
        await _context.ExtractionCandidates.AddAsync(candidate, cancellationToken);
    }

    public void Update(ExtractionCandidate candidate)
    {
        _context.ExtractionCandidates.Update(candidate);
    }

    public async Task<bool> HasCandidatesForEmailAsync(
        Guid emailId,
        CancellationToken cancellationToken = default
    )
    {
        return await _context.ExtractionCandidates.AnyAsync(
            c => c.EmailMessageId == emailId,
            cancellationToken
        );
    }
}
