using MainLedger.Domain.Entities;
using MainLedger.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MainLedger.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for EmailMessage entity.
/// </summary>
public class EmailMessageConfiguration : IEntityTypeConfiguration<EmailMessage>
{
    public void Configure(EntityTypeBuilder<EmailMessage> builder)
    {
        builder.ToTable("email_messages");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(e => e.MessageId)
            .HasColumnName("message_id")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.ThreadId)
            .HasColumnName("thread_id")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.Subject)
            .HasColumnName("subject")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.From)
            .HasColumnName("from")
            .HasMaxLength(255)
            .IsRequired()
            .HasConversion(new EmailAddressConverter());

        builder.Property(e => e.ReceivedAt)
            .HasColumnName("received_at")
            .IsRequired();

        builder.Property(e => e.BodyText)
            .HasColumnName("body_text")
            .IsRequired();

        builder.Property(e => e.ContentHash)
            .HasColumnName("content_hash")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(e => e.IsProcessed)
            .HasColumnName("is_processed")
            .IsRequired();

        builder.Property(e => e.ProcessedAt)
            .HasColumnName("processed_at");

        builder.Property(e => e.Directive)
            .HasColumnName("directive")
            .HasConversion<string?>()
            .HasMaxLength(50);

        builder.Property(e => e.DirectiveReason)
            .HasColumnName("directive_reason")
            .HasMaxLength(1000);

        builder.Property(e => e.MatchedRuleId)
            .HasColumnName("matched_rule_id");

        // Classification fields
        builder.Property(e => e.IsFinancial)
            .HasColumnName("is_financial");

        builder.Property(e => e.Category)
            .HasColumnName("category")
            .HasConversion<string?>()
            .HasMaxLength(50);

        builder.Property(e => e.ClassificationConfidence)
            .HasColumnName("classification_confidence")
            .HasConversion(new ConfidenceConverter());

        builder.Property(e => e.ClassifiedAt)
            .HasColumnName("classified_at");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Relationships
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_email_messages_user_id");

        builder.HasIndex(e => e.MessageId)
            .HasDatabaseName("ix_email_messages_message_id");

        builder.HasIndex(e => e.IsProcessed)
            .HasDatabaseName("ix_email_messages_is_processed");

        builder.HasIndex(e => e.ContentHash)
            .IsUnique()
            .HasDatabaseName("ix_email_messages_content_hash");

        builder.HasIndex(e => e.Directive)
            .HasDatabaseName("ix_email_messages_directive");

        builder.HasIndex(e => e.IsFinancial)
            .HasDatabaseName("ix_email_messages_is_financial");

        builder.HasIndex(e => e.Category)
            .HasDatabaseName("ix_email_messages_category");
    }
}
