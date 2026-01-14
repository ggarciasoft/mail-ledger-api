using MainLedger.Domain.Entities;
using MainLedger.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MainLedger.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for workflow configuration operations.
/// </summary>
public class WorkflowConfigurationRepository : IWorkflowConfigurationRepository
{
    private readonly MailLedgerDbContext _context;

    public WorkflowConfigurationRepository(MailLedgerDbContext context)
    {
        _context = context;
    }

    public async Task<WorkflowConfiguration?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        return await _context.WorkflowConfigurations.FirstOrDefaultAsync(
            wc => wc.UserId == userId,
            cancellationToken
        );
    }

    public async Task AddAsync(
        WorkflowConfiguration configuration,
        CancellationToken cancellationToken = default
    )
    {
        await _context.WorkflowConfigurations.AddAsync(configuration, cancellationToken);
    }

    public void Update(WorkflowConfiguration configuration)
    {
        _context.WorkflowConfigurations.Update(configuration);
    }
}
