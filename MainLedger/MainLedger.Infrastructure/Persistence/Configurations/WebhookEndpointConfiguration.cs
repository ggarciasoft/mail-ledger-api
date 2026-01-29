using MainLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MainLedger.Infrastructure.Persistence.Configurations;

public class WebhookEndpointConfiguration : IEntityTypeConfiguration<WebhookEndpoint>
{
    public void Configure(EntityTypeBuilder<WebhookEndpoint> builder)
    {
        builder.ToTable("webhook_endpoints");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(w => w.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(w => w.Url)
            .HasColumnName("url")
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(w => w.SecretKey)
            .HasColumnName("secret_key")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(w => w.Events)
            .HasColumnName("events")
            .HasConversion(
                v => string.Join(',', v.Select(e => (int)e)),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                      .Select(e => (MainLedger.Domain.Enums.WebhookEventType)int.Parse(e))
                      .ToList())
            .IsRequired();

        builder.Property(w => w.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(w => w.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(w => w.LastTriggeredAt)
            .HasColumnName("last_triggered_at");

        // Relationships
        builder.HasOne(w => w.User)
            .WithMany()
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(w => w.Deliveries)
            .WithOne(d => d.WebhookEndpoint)
            .HasForeignKey(d => d.WebhookEndpointId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(w => w.UserId)
            .HasDatabaseName("ix_webhook_endpoints_user_id");

        builder.HasIndex(w => w.IsActive)
            .HasDatabaseName("ix_webhook_endpoints_is_active");
    }
}
