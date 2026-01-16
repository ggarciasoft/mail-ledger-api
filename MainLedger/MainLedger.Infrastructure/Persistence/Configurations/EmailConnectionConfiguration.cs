using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MainLedger.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for EmailConnection entity.
/// </summary>
public class EmailConnectionConfiguration : IEntityTypeConfiguration<EmailConnection>
{
    public void Configure(EntityTypeBuilder<EmailConnection> builder)
    {
        builder.ToTable("email_connections");

        builder.HasKey(ec => ec.Id);

        builder.Property(ec => ec.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(ec => ec.UserId).HasColumnName("user_id").IsRequired();

        builder
            .Property(ec => ec.Provider)
            .HasColumnName("provider")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(ec => ec.Email).HasColumnName("email").HasMaxLength(255).IsRequired();

        builder
            .Property(ec => ec.EncryptedAccessToken)
            .HasColumnName("encrypted_access_token")
            .IsRequired();

        builder
            .Property(ec => ec.EncryptedRefreshToken)
            .HasColumnName("encrypted_refresh_token")
            .IsRequired();

        builder.Property(ec => ec.TokenExpiresAt).HasColumnName("token_expires_at").IsRequired();

        builder.Property(ec => ec.IsActive).HasColumnName("is_active").IsRequired();

        builder.Property(ec => ec.LastSyncedAt).HasColumnName("last_synced_at");

        builder.Property(ec => ec.ConnectedAt).HasColumnName("connected_at").IsRequired();

        builder.Property(ec => ec.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.Property(ec => ec.UpdatedAt).HasColumnName("updated_at").IsRequired();

        // Relationships
        builder
            .HasOne(ec => ec.User)
            .WithMany()
            .HasForeignKey(ec => ec.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(ec => ec.EmailMessages)
            .WithOne(em => em.EmailConnection)
            .HasForeignKey("EmailConnectionId")
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(ec => ec.UserId).HasDatabaseName("ix_email_connections_user_id");

        builder
            .HasIndex(ec => new { ec.UserId, ec.Provider })
            .IsUnique()
            .HasDatabaseName("ix_email_connections_user_provider");

        builder.HasIndex(ec => ec.IsActive).HasDatabaseName("ix_email_connections_is_active");
    }
}
