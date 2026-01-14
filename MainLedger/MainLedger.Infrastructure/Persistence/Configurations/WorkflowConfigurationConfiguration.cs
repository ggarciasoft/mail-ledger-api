using MainLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MainLedger.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for WorkflowConfiguration entity.
/// </summary>
public class WorkflowConfigurationConfiguration : IEntityTypeConfiguration<WorkflowConfiguration>
{
    public void Configure(EntityTypeBuilder<WorkflowConfiguration> builder)
    {
        builder.ToTable("workflow_configurations");

        builder.HasKey(wc => wc.Id);
        builder.Property(wc => wc.Id).HasColumnName("id");

        builder.Property(wc => wc.UserId).IsRequired().HasColumnName("user_id");

        builder.Property(wc => wc.Mode).IsRequired().HasColumnName("mode").HasConversion<int>();

        builder
            .Property(wc => wc.EmailSyncSchedule)
            .HasMaxLength(100)
            .HasColumnName("email_sync_schedule");

        builder
            .Property(wc => wc.ClassificationSchedule)
            .HasMaxLength(100)
            .HasColumnName("classification_schedule");

        builder
            .Property(wc => wc.ExtractionSchedule)
            .HasMaxLength(100)
            .HasColumnName("extraction_schedule");

        builder
            .Property(wc => wc.PipelineSchedule)
            .HasMaxLength(100)
            .HasColumnName("pipeline_schedule");

        builder
            .Property(wc => wc.EmailSyncBatchSize)
            .IsRequired()
            .HasColumnName("email_sync_batch_size")
            .HasDefaultValue(50);

        builder
            .Property(wc => wc.ClassificationBatchSize)
            .IsRequired()
            .HasColumnName("classification_batch_size")
            .HasDefaultValue(20);

        builder
            .Property(wc => wc.ExtractionBatchSize)
            .IsRequired()
            .HasColumnName("extraction_batch_size")
            .HasDefaultValue(20);

        builder
            .Property(wc => wc.TimeZoneId)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("time_zone_id")
            .HasDefaultValue("UTC");

        builder.Property(wc => wc.CreatedAt).IsRequired().HasColumnName("created_at");

        builder.Property(wc => wc.UpdatedAt).HasColumnName("updated_at");

        // Indexes
        builder
            .HasIndex(wc => wc.UserId)
            .IsUnique()
            .HasDatabaseName("ix_workflow_configurations_user_id");

        // Foreign key
        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(wc => wc.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
