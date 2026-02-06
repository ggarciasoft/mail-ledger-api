using MainLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MainLedger.Infrastructure.Persistence.Configurations;

public class ExternalLoginConfiguration : IEntityTypeConfiguration<ExternalLogin>
{
    public void Configure(EntityTypeBuilder<ExternalLogin> builder)
    {
        builder.ToTable("ExternalLogins");

        builder.HasKey(el => el.Id);

        builder.Property(el => el.UserId)
            .IsRequired();

        builder.Property(el => el.Provider)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(el => el.ProviderUserId)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(el => el.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(el => el.CreatedAt)
            .IsRequired();

        builder.Property(el => el.LastUsedAt);

        // Indexes
        builder.HasIndex(el => new { el.Provider, el.ProviderUserId })
            .IsUnique()
            .HasDatabaseName("IX_ExternalLogins_Provider_ProviderUserId");

        builder.HasIndex(el => el.UserId)
            .HasDatabaseName("IX_ExternalLogins_UserId");

        // Relationships
        builder.HasOne(el => el.User)
            .WithMany()
            .HasForeignKey(el => el.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
