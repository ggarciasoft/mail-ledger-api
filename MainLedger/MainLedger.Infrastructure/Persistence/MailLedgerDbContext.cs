using System.Reflection;
using MainLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MainLedger.Infrastructure.Persistence;

/// <summary>
/// Main database context for MailLedger application.
/// </summary>
public class MailLedgerDbContext : DbContext
{
    public MailLedgerDbContext(DbContextOptions<MailLedgerDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<EmailConnection> EmailConnections => Set<EmailConnection>();
    public DbSet<EmailSyncHistory> EmailSyncHistories => Set<EmailSyncHistory>();
    public DbSet<EmailMessage> EmailMessages => Set<EmailMessage>();
    public DbSet<Rule> Rules => Set<Rule>();
    public DbSet<FinancialRecord> FinancialRecords => Set<FinancialRecord>();
    public DbSet<ExtractionVersion> ExtractionVersions => Set<ExtractionVersion>();
    public DbSet<ExtractionCandidate> ExtractionCandidates => Set<ExtractionCandidate>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Authentication entities
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<ProcessingJob> ProcessingJobs => Set<ProcessingJob>();
    public DbSet<WorkflowConfiguration> WorkflowConfigurations => Set<WorkflowConfiguration>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

    // Subscription entities
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<UserSubscription> UserSubscriptions => Set<UserSubscription>();
    public DbSet<EmailNotification> EmailNotifications => Set<EmailNotification>();

    // Category entity
    public DbSet<Category> Categories => Set<Category>();

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
