using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MainLedger.Infrastructure.Persistence.Configurations;

public class UserSubscriptionConfiguration : IEntityTypeConfiguration<UserSubscription>
{
    public void Configure(EntityTypeBuilder<UserSubscription> builder)
    {
        builder.ToTable("user_subscriptions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).HasColumnName("id").IsRequired();

        builder.Property(s => s.UserId).HasColumnName("user_id").IsRequired();

        builder
            .Property(s => s.SubscriptionPlanId)
            .HasColumnName("subscription_plan_id")
            .IsRequired();

        builder.Property(s => s.StartDate).HasColumnName("start_date").IsRequired();

        builder.Property(s => s.EndDate).HasColumnName("end_date");

        builder
            .Property(s => s.Status)
            .HasColumnName("status")
            .HasMaxLength(50)
            .HasConversion(
                v => v.ToString(),
                v => (SubscriptionStatus)Enum.Parse(typeof(SubscriptionStatus), v)
            )
            .IsRequired();

        builder
            .Property(s => s.EmailsProcessedThisMonth)
            .HasColumnName("emails_processed_this_month")
            .IsRequired()
            .HasDefaultValue(0);

        builder
            .Property(s => s.CurrentPeriodStart)
            .HasColumnName("current_period_start")
            .IsRequired();

        builder.Property(s => s.CurrentPeriodEnd).HasColumnName("current_period_end").IsRequired();

        builder.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").IsRequired();

        // Relationships
        builder
            .HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(s => s.SubscriptionPlan)
            .WithMany(p => p.UserSubscriptions)
            .HasForeignKey(s => s.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(s => s.UserId).IsUnique(); // One subscription per user
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => new { s.Status, s.EndDate }); // For finding expired subscriptions
    }
}
