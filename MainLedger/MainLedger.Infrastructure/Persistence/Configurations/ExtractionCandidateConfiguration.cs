using MainLedger.Domain.Entities;
using MainLedger.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MainLedger.Infrastructure.Persistence.Configurations;

public class ExtractionCandidateConfiguration : IEntityTypeConfiguration<ExtractionCandidate>
{
    public void Configure(EntityTypeBuilder<ExtractionCandidate> builder)
    {
        builder.ToTable("extraction_candidates");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(e => e.EmailMessageId)
            .HasColumnName("email_message_id")
            .IsRequired();

        builder.Property(e => e.ExtractionVersionId)
            .HasColumnName("extraction_version_id")
            .IsRequired();

        // Core transaction data
        builder.OwnsOne(e => e.Amount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("amount")
                .HasColumnType("decimal(18,2)");
            
            money.Property(m => m.Currency)
                .HasColumnName("currency")
                .HasConversion<string>()
                .HasMaxLength(3);
        });

        builder.Property(e => e.TransactionDate)
            .HasColumnName("transaction_date");

        builder.Property(e => e.Merchant)
            .HasColumnName("merchant")
            .HasMaxLength(500);

        // Account information
        builder.Property(e => e.SourceAccount)
            .HasColumnName("source_account")
            .HasConversion(new AccountNumberConverter())
            .HasMaxLength(100);

        builder.Property(e => e.TargetAccount)
            .HasColumnName("target_account")
            .HasConversion(new AccountNumberConverter())
            .HasMaxLength(100);

        builder.Property(e => e.SourceBank)
            .HasColumnName("source_bank")
            .HasConversion(new BankProviderConverter())
            .HasColumnType("jsonb");

        builder.Property(e => e.TargetBank)
            .HasColumnName("target_bank")
            .HasConversion(new BankProviderConverter())
            .HasColumnType("jsonb");

        // Additional financial details
        builder.OwnsOne(e => e.Fees, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("fees_amount")
                .HasColumnType("decimal(18,2)");
            
            money.Property(m => m.Currency)
                .HasColumnName("fees_currency")
                .HasConversion<string>()
                .HasMaxLength(3);
        });

        builder.OwnsOne(e => e.Tax, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("tax_amount")
                .HasColumnType("decimal(18,2)");
            
            money.Property(m => m.Currency)
                .HasColumnName("tax_currency")
                .HasConversion<string>()
                .HasMaxLength(3);
        });

        builder.Property(e => e.ReferenceId)
            .HasColumnName("reference_id")
            .HasMaxLength(200);

        // Confidence scores
        builder.Property(e => e.AmountConfidence)
            .HasColumnName("amount_confidence")
            .HasConversion(new ConfidenceConverter());

        builder.Property(e => e.DateConfidence)
            .HasColumnName("date_confidence")
            .HasConversion(new ConfidenceConverter());

        builder.Property(e => e.MerchantConfidence)
            .HasColumnName("merchant_confidence")
            .HasConversion(new ConfidenceConverter());

        // Status tracking
        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.RejectionReason)
            .HasColumnName("rejection_reason")
            .HasMaxLength(1000);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.ConfirmedAt)
            .HasColumnName("confirmed_at");

        // Indexes
        builder.HasIndex(e => e.EmailMessageId)
            .HasDatabaseName("ix_extraction_candidates_email_message_id");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("ix_extraction_candidates_status");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("ix_extraction_candidates_created_at");
    }
}
