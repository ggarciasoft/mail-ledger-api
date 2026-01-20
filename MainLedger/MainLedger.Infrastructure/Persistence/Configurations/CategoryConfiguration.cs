using MainLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MainLedger.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("id").IsRequired();


        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(100).IsRequired();

        builder.Property(c => c.Description).HasColumnName("description").HasMaxLength(500);

        builder.Property(c => c.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").IsRequired();
    }
}
