using MainLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace MainLedger.Infrastructure.Persistence;

/// <summary>
/// Main database context for MailLedger application.
/// </summary>
public class MailLedgerDbContext : DbContext
{
    public MailLedgerDbContext(DbContextOptions<MailLedgerDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<GmailConnection> GmailConnections => Set<GmailConnection>();
    public DbSet<EmailMessage> EmailMessages => Set<EmailMessage>();
    public DbSet<Rule> Rules => Set<Rule>();
    public DbSet<FinancialRecord> FinancialRecords => Set<FinancialRecord>();
    public DbSet<ExtractionVersion> ExtractionVersions => Set<ExtractionVersion>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from the current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Configure naming conventions (snake_case is handled in individual configurations)
        // Additional global configurations can be added here if needed
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // You can add audit logging logic here if needed
        // For now, just call the base implementation
        return await base.SaveChangesAsync(cancellationToken);
    }
}
