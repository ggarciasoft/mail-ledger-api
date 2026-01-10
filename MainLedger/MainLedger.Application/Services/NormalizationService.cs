using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using MainLedger.Application.Common.Interfaces;
using MainLedger.Application.Common.Models;
using MainLedger.Domain.Entities;

namespace MainLedger.Application.Services;

/// <summary>
/// Service for normalizing and validating extracted financial data.
/// Ensures data consistency, quality, and deduplication.
/// </summary>
public class NormalizationService : INormalizationService
{
    private static readonly HashSet<string> ValidCurrencies = new()
    {
        "USD",
        "EUR",
        "GBP",
        "DOP",
        "CAD",
        "AUD",
        "JPY",
        "CHF",
        "CNY",
        "INR",
        "MXN",
        "BRL",
    };

    private static readonly string[] MerchantSuffixes = new[]
    {
        "LLC",
        "INC",
        "CORP",
        "LTD",
        "CO",
        "CORPORATION",
        "INCORPORATED",
        "LIMITED",
        "COMPANY",
    };

    public async Task<NormalizationResult> NormalizeExtractionAsync(
        ExtractionResult extraction,
        EmailMessage email,
        CancellationToken cancellationToken = default
    )
    {
        var errors = new List<NormalizationError>();
        var warnings = new List<NormalizationWarning>();

        // Normalize currency
        var normalizedCurrency = NormalizeCurrency(
            extraction.Currency,
            extraction.Amount,
            errors,
            warnings
        );

        // Normalize amount
        var normalizedAmount = NormalizeAmount(extraction.Amount, errors);

        // Normalize date
        var normalizedDate = NormalizeDate(
            extraction.TransactionDate,
            email.ReceivedAt,
            errors,
            warnings
        );

        // Normalize merchant
        var normalizedMerchant = NormalizeMerchant(extraction.Merchant, warnings);

        // Normalize account numbers
        var normalizedSourceAccount = NormalizeAccountNumber(
            extraction.SourceAccount,
            "SourceAccount",
            warnings
        );
        var normalizedTargetAccount = NormalizeAccountNumber(
            extraction.TargetAccount,
            "TargetAccount",
            warnings
        );

        // Normalize bank names
        var normalizedSourceBank = NormalizeBankName(extraction.SourceBank);
        var normalizedTargetBank = NormalizeBankName(extraction.TargetBank);

        // Normalize fees and tax
        var normalizedFees = NormalizeAmount(extraction.Fees, errors, isOptional: true);
        var normalizedTax = NormalizeAmount(extraction.Tax, errors, isOptional: true);

        // Generate deduplication hash
        var deduplicationHash = GenerateDeduplicationHash(
            email.ContentHash,
            normalizedAmount,
            normalizedDate
        );

        return await Task.FromResult(
            new NormalizationResult
            {
                NormalizedAmount = normalizedAmount,
                NormalizedCurrency = normalizedCurrency,
                NormalizedDate = normalizedDate,
                NormalizedMerchant = normalizedMerchant,
                NormalizedSourceAccount = normalizedSourceAccount,
                NormalizedTargetAccount = normalizedTargetAccount,
                NormalizedSourceBank = normalizedSourceBank,
                NormalizedTargetBank = normalizedTargetBank,
                NormalizedFees = normalizedFees,
                NormalizedTax = normalizedTax,
                ReferenceId = extraction.ReferenceId,
                DeduplicationHash = deduplicationHash,
                Errors = errors,
                Warnings = warnings,
                AmountConfidence = extraction.AmountConfidence,
                DateConfidence = extraction.DateConfidence,
                MerchantConfidence = extraction.MerchantConfidence,
            }
        );
    }

    /// <summary>
    /// Normalizes currency code.
    /// </summary>
    private string? NormalizeCurrency(
        string? currency,
        decimal? amount,
        List<NormalizationError> errors,
        List<NormalizationWarning> warnings
    )
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            // Default to USD if amount is present
            if (amount.HasValue)
            {
                warnings.Add(
                    new NormalizationWarning(
                        "Currency",
                        "Currency missing, defaulting to USD",
                        null,
                        "USD"
                    )
                );
                return "USD";
            }
            return null;
        }

        var normalized = currency.Trim().ToUpperInvariant();

        if (!ValidCurrencies.Contains(normalized))
        {
            warnings.Add(
                new NormalizationWarning(
                    "Currency",
                    $"Unknown currency code: {normalized}. Please verify.",
                    currency,
                    normalized
                )
            );
        }

        return normalized;
    }

    /// <summary>
    /// Normalizes and validates amount.
    /// </summary>
    private decimal? NormalizeAmount(
        decimal? amount,
        List<NormalizationError> errors,
        bool isOptional = false
    )
    {
        if (amount.GetValueOrDefault() == 0)
        {
            if (!isOptional)
            {
                errors.Add(new NormalizationError("Amount", "Amount is required", null));
            }
            return null;
        }

        // Log the actual amount value for debugging
        Console.WriteLine($"[DEBUG] Normalizing amount: {amount.Value} (IsOptional: {isOptional})");

        // Validate positive
        if (amount.Value <= 0)
        {
            errors.Add(
                new NormalizationError(
                    "Amount",
                    $"Amount must be positive (received: {amount.Value})",
                    amount.Value.ToString(CultureInfo.InvariantCulture)
                )
            );
            return null;
        }

        // Validate reasonable (< $1,000,000)
        if (amount.Value > 1_000_000)
        {
            errors.Add(
                new NormalizationError(
                    "Amount",
                    "Amount exceeds reasonable limit ($1,000,000)",
                    amount.Value.ToString(CultureInfo.InvariantCulture)
                )
            );
            return null;
        }

        // Round to 2 decimal places
        return Math.Round(amount.Value, 2);
    }

    /// <summary>
    /// Normalizes date to UTC and validates.
    /// </summary>
    private DateTime? NormalizeDate(
        DateTime? transactionDate,
        DateTime emailReceivedAt,
        List<NormalizationError> errors,
        List<NormalizationWarning> warnings
    )
    {
        if (!transactionDate.HasValue)
        {
            // Default to email received date
            warnings.Add(
                new NormalizationWarning(
                    "TransactionDate",
                    "Transaction date missing, using email received date",
                    null,
                    emailReceivedAt.ToString("yyyy-MM-dd")
                )
            );
            return emailReceivedAt.ToUniversalTime();
        }

        var normalized = transactionDate.Value.ToUniversalTime();

        // Validate not in future (with 24h tolerance)
        var futureLimit = DateTime.UtcNow.AddHours(24);
        if (normalized > futureLimit)
        {
            errors.Add(
                new NormalizationError(
                    "TransactionDate",
                    "Transaction date cannot be in the future",
                    transactionDate.Value.ToString("yyyy-MM-dd")
                )
            );
            return null;
        }

        // Validate not too old (> 10 years)
        var oldLimit = DateTime.UtcNow.AddYears(-10);
        if (normalized < oldLimit)
        {
            warnings.Add(
                new NormalizationWarning(
                    "TransactionDate",
                    "Transaction date is more than 10 years old. Please verify.",
                    transactionDate.Value.ToString("yyyy-MM-dd"),
                    normalized.ToString("yyyy-MM-dd")
                )
            );
        }

        return normalized;
    }

    /// <summary>
    /// Normalizes merchant name.
    /// </summary>
    private string? NormalizeMerchant(string? merchant, List<NormalizationWarning> warnings)
    {
        if (string.IsNullOrWhiteSpace(merchant))
        {
            return null;
        }

        // Trim whitespace
        var normalized = merchant.Trim();

        // Remove special characters (*, #, etc.)
        normalized = Regex.Replace(normalized, @"[*#@!$%^&()]", "");

        // Remove extra whitespace
        normalized = Regex.Replace(normalized, @"\s+", " ");

        // Convert to title case
        var textInfo = CultureInfo.CurrentCulture.TextInfo;
        normalized = textInfo.ToTitleCase(normalized.ToLower());

        // Remove common suffixes
        foreach (var suffix in MerchantSuffixes)
        {
            var pattern = $@"\s+{suffix}\.?$";
            if (Regex.IsMatch(normalized, pattern, RegexOptions.IgnoreCase))
            {
                normalized = Regex.Replace(normalized, pattern, "", RegexOptions.IgnoreCase).Trim();
            }
        }

        // Max length 200 characters
        if (normalized.Length > 200)
        {
            warnings.Add(
                new NormalizationWarning(
                    "Merchant",
                    "Merchant name truncated to 200 characters",
                    merchant,
                    normalized.Substring(0, 200)
                )
            );
            normalized = normalized.Substring(0, 200);
        }

        return normalized;
    }

    /// <summary>
    /// Normalizes account number to masked format.
    /// </summary>
    private string? NormalizeAccountNumber(
        string? accountNumber,
        string fieldName,
        List<NormalizationWarning> warnings
    )
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
        {
            return null;
        }

        // Remove spaces and dashes
        var normalized = accountNumber.Replace(" ", "").Replace("-", "");

        // Validate length (4-20 characters)
        if (normalized.Length < 4 || normalized.Length > 20)
        {
            warnings.Add(
                new NormalizationWarning(
                    fieldName,
                    $"Account number length unusual ({normalized.Length} characters)",
                    accountNumber,
                    normalized
                )
            );
        }

        // Ensure masked format (***XXXX)
        if (!normalized.StartsWith("***") && !normalized.StartsWith("*"))
        {
            // If not already masked, mask all but last 4 digits
            if (normalized.Length > 4)
            {
                var lastFour = normalized.Substring(normalized.Length - 4);
                normalized = "***" + lastFour;
                warnings.Add(
                    new NormalizationWarning(
                        fieldName,
                        "Account number auto-masked for security",
                        accountNumber,
                        normalized
                    )
                );
            }
        }

        return normalized;
    }

    /// <summary>
    /// Normalizes bank name.
    /// </summary>
    private string? NormalizeBankName(string? bankName)
    {
        if (string.IsNullOrWhiteSpace(bankName))
        {
            return null;
        }

        // Trim and convert to uppercase for consistency
        return bankName.Trim().ToUpperInvariant();
    }

    /// <summary>
    /// Generates SHA256 hash for deduplication.
    /// </summary>
    private string GenerateDeduplicationHash(
        string emailContentHash,
        decimal? amount,
        DateTime? date
    )
    {
        var hashInput = new StringBuilder();
        hashInput.Append(emailContentHash);

        if (amount.HasValue)
        {
            hashInput.Append("|");
            hashInput.Append(amount.Value.ToString("F2", CultureInfo.InvariantCulture));
        }

        if (date.HasValue)
        {
            hashInput.Append("|");
            hashInput.Append(date.Value.ToString("yyyy-MM-dd"));
        }

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(hashInput.ToString()));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
