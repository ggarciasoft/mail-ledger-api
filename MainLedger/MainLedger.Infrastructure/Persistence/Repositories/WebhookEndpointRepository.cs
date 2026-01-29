using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MainLedger.Infrastructure.Persistence.Repositories;

public class WebhookEndpointRepository : IWebhookEndpointRepository
{
    private readonly MailLedgerDbContext _context;

    public WebhookEndpointRepository(MailLedgerDbContext context)
    {
        _context = context;
    }

    public async Task<WebhookEndpoint?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.WebhookEndpoints
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<List<WebhookEndpoint>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.WebhookEndpoints
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<WebhookEndpoint>> GetActiveByUserIdAndEventTypeAsync(
        Guid userId, 
        WebhookEventType eventType, 
        CancellationToken cancellationToken = default)
    {
        return await _context.WebhookEndpoints
            .Where(w => w.UserId == userId && w.IsActive && w.Events.Contains(eventType))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(WebhookEndpoint webhookEndpoint, CancellationToken cancellationToken = default)
    {
        await _context.WebhookEndpoints.AddAsync(webhookEndpoint, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(WebhookEndpoint webhookEndpoint, CancellationToken cancellationToken = default)
    {
        _context.WebhookEndpoints.Update(webhookEndpoint);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(WebhookEndpoint webhookEndpoint, CancellationToken cancellationToken = default)
    {
        _context.WebhookEndpoints.Remove(webhookEndpoint);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.WebhookEndpoints
            .CountAsync(w => w.UserId == userId, cancellationToken);
    }
}
