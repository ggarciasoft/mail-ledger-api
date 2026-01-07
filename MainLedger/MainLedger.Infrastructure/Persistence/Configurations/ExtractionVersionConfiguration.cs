using MainLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MainLedger.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for ExtractionVersion entity.
/// </summary>
public class ExtractionVersionConfiguration : IEntityTypeConfiguration<ExtractionVersion>
{
    public void Configure(EntityTypeBuilder<ExtractionVersion> builder)
    {
        builder.ToTable("extraction_versions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(e => e.Version)
            .HasColumnName("version")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.ModelName)
            .HasColumnName("model_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.PromptHash)
            .HasColumnName("prompt_hash")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.DeprecatedAt)
            .HasColumnName("deprecated_at");

        // Indexes
        builder.HasIndex(e => e.Version)
            .IsUnique()
            .HasDatabaseName("ix_extraction_versions_version");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("ix_extraction_versions_is_active");
    }
}
