using MainLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MainLedger.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for ApiKey entity.
/// </summary>
public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("api_keys");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(a => a.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(a => a.KeyHash)
            .HasColumnName("key_hash")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(a => a.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(a => a.Scopes)
            .HasColumnName("scopes")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(a => a.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(a => a.ExpiresAt)
            .HasColumnName("expires_at");

        builder.Property(a => a.LastUsedAt)
            .HasColumnName("last_used_at");

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Relationships
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(a => a.KeyHash)
            .IsUnique()
            .HasDatabaseName("ix_api_keys_key_hash");

        builder.HasIndex(a => a.UserId)
            .HasDatabaseName("ix_api_keys_user_id");

        builder.HasIndex(a => a.IsActive)
            .HasDatabaseName("ix_api_keys_is_active");

        builder.HasIndex(a => new { a.UserId, a.IsActive })
            .HasDatabaseName("ix_api_keys_user_id_is_active");
    }
}
