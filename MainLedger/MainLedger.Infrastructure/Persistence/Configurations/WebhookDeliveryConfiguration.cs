using MainLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MainLedger.Infrastructure.Persistence.Configurations;

public class WebhookDeliveryConfiguration : IEntityTypeConfiguration<WebhookDelivery>
{
    public void Configure(EntityTypeBuilder<WebhookDelivery> builder)
    {
        builder.ToTable("webhook_deliveries");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(d => d.WebhookEndpointId)
            .HasColumnName("webhook_endpoint_id")
            .IsRequired();

        builder.Property(d => d.EventType)
            .HasColumnName("event_type")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(d => d.Payload)
            .HasColumnName("payload")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(d => d.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(d => d.AttemptCount)
            .HasColumnName("attempt_count")
            .IsRequired();

        builder.Property(d => d.LastAttemptAt)
            .HasColumnName("last_attempt_at");

        builder.Property(d => d.ResponseStatusCode)
            .HasColumnName("response_status_code");

        builder.Property(d => d.ResponseBody)
            .HasColumnName("response_body")
            .HasMaxLength(1000);

        builder.Property(d => d.ErrorMessage)
            .HasColumnName("error_message")
            .HasMaxLength(2000);

        builder.Property(d => d.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Relationships
        builder.HasOne(d => d.WebhookEndpoint)
            .WithMany(w => w.Deliveries)
            .HasForeignKey(d => d.WebhookEndpointId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(d => d.WebhookEndpointId)
            .HasDatabaseName("ix_webhook_deliveries_webhook_endpoint_id");

        builder.HasIndex(d => d.Status)
            .HasDatabaseName("ix_webhook_deliveries_status");

        builder.HasIndex(d => d.CreatedAt)
            .HasDatabaseName("ix_webhook_deliveries_created_at");
    }
}
