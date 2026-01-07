using MainLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MainLedger.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for Rule entity.
/// </summary>
public class RuleConfiguration : IEntityTypeConfiguration<Rule>
{
    public void Configure(EntityTypeBuilder<Rule> builder)
    {
        builder.ToTable("rules");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(r => r.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(r => r.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.SenderPattern)
            .HasColumnName("sender_pattern")
            .HasMaxLength(500);

        builder.Property(r => r.SubjectPattern)
            .HasColumnName("subject_pattern")
            .HasMaxLength(500);

        builder.Property(r => r.KeywordPattern)
            .HasColumnName("keyword_pattern")
            .HasMaxLength(500);

        builder.Property(r => r.LabelPattern)
            .HasColumnName("label_pattern")
            .HasMaxLength(500);

        builder.Property(r => r.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(r => r.Priority)
            .HasColumnName("priority")
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(r => r.UpdatedAt)
            .HasColumnName("updated_at");

        // Relationships
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(r => r.UserId)
            .HasDatabaseName("ix_rules_user_id");

        builder.HasIndex(r => r.IsActive)
            .HasDatabaseName("ix_rules_is_active");

        builder.HasIndex(r => r.Priority)
            .HasDatabaseName("ix_rules_priority");
    }
}
