using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MainLedger.Infrastructure.Persistence.Repositories;

public class WebhookDeliveryRepository : IWebhookDeliveryRepository
{
    private readonly MailLedgerDbContext _context;

    public WebhookDeliveryRepository(MailLedgerDbContext context)
    {
        _context = context;
    }

    public async Task<WebhookDelivery?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.WebhookDeliveries
            .Include(d => d.WebhookEndpoint)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<(List<WebhookDelivery> Deliveries, int TotalCount)> GetByEndpointIdAsync(
        Guid endpointId,
        WebhookDeliveryStatus? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.WebhookDeliveries
            .Where(d => d.WebhookEndpointId == endpointId);

        if (status.HasValue)
        {
            query = query.Where(d => d.Status == status.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(d => d.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(d => d.CreatedAt <= toDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var deliveries = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (deliveries, totalCount);
    }

    public async Task AddAsync(WebhookDelivery webhookDelivery, CancellationToken cancellationToken = default)
    {
        await _context.WebhookDeliveries.AddAsync(webhookDelivery, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(WebhookDelivery webhookDelivery, CancellationToken cancellationToken = default)
    {
        _context.WebhookDeliveries.Update(webhookDelivery);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteOlderThanAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
    {
        var oldDeliveries = await _context.WebhookDeliveries
            .Where(d => d.CreatedAt < cutoffDate)
            .ToListAsync(cancellationToken);

        _context.WebhookDeliveries.RemoveRange(oldDeliveries);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<WebhookDelivery>> GetStuckPendingDeliveriesAsync(DateTime cutoffTime, CancellationToken cancellationToken = default)
    {
        return await _context.WebhookDeliveries
            .Include(d => d.WebhookEndpoint)
            .Where(d => d.Status == WebhookDeliveryStatus.Pending && d.CreatedAt < cutoffTime)
            .OrderBy(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
