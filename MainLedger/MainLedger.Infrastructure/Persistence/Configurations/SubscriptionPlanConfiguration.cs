using MainLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MainLedger.Infrastructure.Persistence.Configurations;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("subscription_plans");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).HasColumnName("id").IsRequired();

        builder.Property(p => p.Name).HasColumnName("name").HasMaxLength(100).IsRequired();

        builder
            .Property(p => p.Description)
            .HasColumnName("description")
            .HasMaxLength(500)
            .IsRequired();

        builder
            .Property(p => p.MonthlyPrice)
            .HasColumnName("monthly_price")
            .HasPrecision(10, 2)
            .IsRequired();

        builder
            .Property(p => p.MonthlyEmailLimit)
            .HasColumnName("monthly_email_limit")
            .IsRequired();

        builder.Property(p => p.MaxEmailAccounts).HasColumnName("max_email_accounts").IsRequired();

        builder.Property(p => p.MaxApiKeys).HasColumnName("max_api_keys").IsRequired();

        builder
            .Property(p => p.HistoryRetentionDays)
            .HasColumnName("history_retention_days")
            .IsRequired();

        builder.Property(p => p.CanExport).HasColumnName("can_export").IsRequired();

        builder
            .Property(p => p.CanUseWorkflowAutomation)
            .HasColumnName("can_use_workflow_automation")
            .IsRequired();

        builder.Property(p => p.CanUseWebhooks).HasColumnName("can_use_webhooks").IsRequired();

        builder.Property(p => p.MaxWebhooks).HasColumnName("max_webhooks").IsRequired();

        builder
            .Property(p => p.CanUseBulkOperations)
            .HasColumnName("can_use_bulk_operations")
            .IsRequired();

        builder.Property(p => p.IsActive).HasColumnName("is_active").IsRequired();

        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(p => p.Name).IsUnique();
        builder.HasIndex(p => p.IsActive);
    }
}
