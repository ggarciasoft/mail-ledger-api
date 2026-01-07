using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;
using MainLedger.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MainLedger.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for FinancialRecord entity.
/// This is the most complex configuration as it represents the canonical financial event model.
/// </summary>
public class FinancialRecordConfiguration : IEntityTypeConfiguration<FinancialRecord>
{
    public void Configure(EntityTypeBuilder<FinancialRecord> builder)
    {
        builder.ToTable("financial_records");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(f => f.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(f => f.EmailMessageId)
            .HasColumnName("email_message_id")
            .IsRequired();

        builder.Property(f => f.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // Money stored as owned entity type
        builder.OwnsOne(f => f.Amount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("amount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("currency")
                .HasConversion<string>()
                .HasMaxLength(10)
                .IsRequired();
        });

        builder.Property(f => f.Direction)
            .HasColumnName("direction")
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(f => f.Merchant)
            .HasColumnName("merchant")
            .HasMaxLength(200);

        builder.Property(f => f.SourceAccount)
            .HasColumnName("source_account")
            .HasMaxLength(50)
            .HasConversion(new AccountNumberConverter());

        builder.Property(f => f.SourceBank)
            .HasColumnName("source_bank")
            .HasMaxLength(500)
            .HasConversion(new BankProviderConverter());

        builder.Property(f => f.TargetAccount)
            .HasColumnName("target_account")
            .HasMaxLength(50)
            .HasConversion(new AccountNumberConverter());

        builder.Property(f => f.TargetBank)
            .HasColumnName("target_bank")
            .HasMaxLength(500)
            .HasConversion(new BankProviderConverter());

        builder.Property(f => f.TransactionDate)
            .HasColumnName("transaction_date")
            .IsRequired();

        // Tax amount stored as owned entity type
        builder.OwnsOne(f => f.TaxAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("tax_amount")
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasColumnName("tax_currency")
                .HasConversion<string>()
                .HasMaxLength(10);
        });

        // Fee amount stored as owned entity type
        builder.OwnsOne(f => f.FeeAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("fee_amount")
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasColumnName("fee_currency")
                .HasConversion<string>()
                .HasMaxLength(10);
        });

        builder.Property(f => f.Confidence)
            .HasColumnName("confidence")
            .IsRequired()
            .HasConversion(new ConfidenceConverter());

        builder.Property(f => f.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(f => f.ConfirmedAt)
            .HasColumnName("confirmed_at");

        builder.Property(f => f.RejectedAt)
            .HasColumnName("rejected_at");

        builder.Property(f => f.ExtractionVersion)
            .HasColumnName("extraction_version")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(f => f.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Relationships
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<EmailMessage>()
            .WithMany()
            .HasForeignKey(f => f.EmailMessageId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(f => f.UserId)
            .HasDatabaseName("ix_financial_records_user_id");

        builder.HasIndex(f => f.Status)
            .HasDatabaseName("ix_financial_records_status");

        builder.HasIndex(f => f.TransactionDate)
            .HasDatabaseName("ix_financial_records_transaction_date");

        builder.HasIndex(f => f.EmailMessageId)
            .IsUnique()
            .HasDatabaseName("ix_financial_records_email_message_id");

        // Composite index for common queries
        builder.HasIndex(f => new { f.UserId, f.Status, f.TransactionDate })
            .HasDatabaseName("ix_financial_records_user_status_date");
    }
}
