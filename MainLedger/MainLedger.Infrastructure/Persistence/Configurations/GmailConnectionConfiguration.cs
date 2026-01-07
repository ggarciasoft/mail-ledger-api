using MainLedger.Domain.Entities;
using MainLedger.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MainLedger.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for GmailConnection entity.
/// </summary>
public class GmailConnectionConfiguration : IEntityTypeConfiguration<GmailConnection>
{
    public void Configure(EntityTypeBuilder<GmailConnection> builder)
    {
        builder.ToTable("gmail_connections");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(g => g.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(g => g.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired()
            .HasConversion(new EmailAddressConverter());

        builder.Property(g => g.RefreshTokenHash)
            .HasColumnName("refresh_token_hash")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(g => g.LastSyncedAt)
            .HasColumnName("last_synced_at");

        builder.Property(g => g.HistoryId)
            .HasColumnName("history_id")
            .HasMaxLength(100);

        builder.Property(g => g.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(g => g.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Relationships
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(g => g.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(g => g.UserId)
            .HasDatabaseName("ix_gmail_connections_user_id");

        builder.HasIndex(g => g.IsActive)
            .HasDatabaseName("ix_gmail_connections_is_active");
    }
}
