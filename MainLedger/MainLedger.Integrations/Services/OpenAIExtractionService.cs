using System.Text;
using System.Text.Json;
using MainLedger.Application.Common.Interfaces;
using MainLedger.Application.Common.Models;
using MainLedger.Domain.Entities;
using MainLedger.Domain.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace MainLedger.Integrations.Services;

/// <summary>
/// Extraction service using OpenAI GPT models to extract structured financial data.
/// </summary>
public class OpenAIExtractionService : IExtractionService
{
    private readonly OpenAISettings _settings;
    private readonly ILogger<OpenAIExtractionService> _logger;
    private readonly ChatClient _chatClient;

    public OpenAIExtractionService(
        IOptions<OpenAISettings> settings,
        ILogger<OpenAIExtractionService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _chatClient = new ChatClient(_settings.Model, _settings.ApiKey);
    }

    public async Task<ExtractionResult> ExtractFinancialDataAsync(
        EmailMessage email,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Truncate body for cost control
            var body = TruncateBody(email.BodyText, _settings.MaxBodyLength);

            // Build extraction prompt
            var prompt = BuildExtractionPrompt(email.Subject, email.From.Value, body, email.Category?.ToString());

            _logger.LogDebug("Extracting financial data from email {MessageId}", email.MessageId);

            // Call OpenAI API
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(GetSystemPrompt()),
                new UserChatMessage(prompt)
            };

            var options = new ChatCompletionOptions
            {
                MaxOutputTokenCount = _settings.MaxTokens,
                Temperature = (float)_settings.Temperature,
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
            };

            var response = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);

            // Parse response
            var result = ParseExtractionResponse(response.Value.Content[0].Text);

            _logger.LogInformation(
                "Email {MessageId} extraction complete: Amount={Amount} {Currency}, Merchant={Merchant}",
                email.MessageId, result.Amount, result.Currency, result.Merchant);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract financial data from email {MessageId}", email.MessageId);
            
            // Return empty result on error
            return new ExtractionResult
            {
                Reasoning = $"Extraction failed: {ex.Message}",
                HasAmbiguities = true
            };
        }
    }

    private string GetSystemPrompt()
    {
        return @"You are an expert financial data extraction system. Your task is to extract structured financial information from email text.

Extract the following fields (set to null if not found or unclear):
- amount: The primary transaction amount (number only, no currency symbol)
- currency: Currency code (USD, EUR, DOP, etc.)
- transactionDate: Date in YYYY-MM-DD format
- merchant: Merchant or payee name
- sourceAccount: Source account (masked format like '***1234')
- targetAccount: Target account for transfers (masked format)
- sourceBank: Source bank name
- targetBank: Target bank name
- fees: Any fees charged (number only)
- tax: Tax amount (ITBIS, VAT, etc.)
- referenceId: Transaction reference or confirmation number

Also provide confidence scores (0.0 to 1.0) for:
- amountConfidence
- dateConfidence
- merchantConfidence

Respond ONLY with valid JSON in this exact format:
{
  ""amount"": 100.50,
  ""currency"": ""USD"",
  ""transactionDate"": ""2026-01-07"",
  ""merchant"": ""Amazon"",
  ""sourceAccount"": ""***1234"",
  ""targetAccount"": null,
  ""sourceBank"": ""Chase"",
  ""targetBank"": null,
  ""fees"": 2.50,
  ""tax"": 0.00,
  ""referenceId"": ""TXN-12345"",
  ""amountConfidence"": 0.95,
  ""dateConfidence"": 0.90,
  ""merchantConfidence"": 0.85,
  ""reasoning"": ""Clear payment confirmation with all details"",
  ""hasAmbiguities"": false
}

IMPORTANT:
- If a field is not found or unclear, set it to null
- Be conservative with confidence scores
- Set hasAmbiguities to true if any important field is missing or unclear
- Extract amounts as numbers without currency symbols
- Use ISO date format (YYYY-MM-DD)";
    }

    private string BuildExtractionPrompt(string subject, string from, string body, string? category)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Extract financial data from this email:");
        sb.AppendLine();
        sb.AppendLine($"From: {from}");
        sb.AppendLine($"Subject: {subject}");
        
        if (!string.IsNullOrWhiteSpace(category))
        {
            sb.AppendLine($"Category: {category}");
        }
        
        sb.AppendLine();
        sb.AppendLine("Body:");
        sb.AppendLine(body);
        
        return sb.ToString();
    }

    private ExtractionResult ParseExtractionResponse(string jsonResponse)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var response = JsonSerializer.Deserialize<ExtractionResponse>(jsonResponse, options);

            if (response == null)
            {
                throw new InvalidOperationException("Failed to deserialize extraction response");
            }

            return new ExtractionResult
            {
                Amount = response.Amount,
                Currency = response.Currency,
                TransactionDate = ParseDate(response.TransactionDate),
                Merchant = response.Merchant,
                SourceAccount = response.SourceAccount,
                TargetAccount = response.TargetAccount,
                SourceBank = response.SourceBank,
                TargetBank = response.TargetBank,
                Fees = response.Fees,
                Tax = response.Tax,
                ReferenceId = response.ReferenceId,
                AmountConfidence = Math.Clamp(response.AmountConfidence, 0.0, 1.0),
                DateConfidence = Math.Clamp(response.DateConfidence, 0.0, 1.0),
                MerchantConfidence = Math.Clamp(response.MerchantConfidence, 0.0, 1.0),
                Reasoning = response.Reasoning ?? string.Empty,
                HasAmbiguities = response.HasAmbiguities
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse extraction response: {Response}", jsonResponse);
            
            return new ExtractionResult
            {
                Reasoning = $"Parse error: {ex.Message}",
                HasAmbiguities = true
            };
        }
    }

    private DateTime? ParseDate(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
            return null;

        if (DateTime.TryParse(dateString, out var date))
            return date.ToUniversalTime();

        return null;
    }

    private string TruncateBody(string body, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(body))
            return string.Empty;

        if (body.Length <= maxLength)
            return body;

        return body.Substring(0, maxLength) + "\n\n[... truncated for length ...]";
    }

    // Internal model for JSON deserialization
    private class ExtractionResponse
    {
        public decimal? Amount { get; set; }
        public string? Currency { get; set; }
        public string? TransactionDate { get; set; }
        public string? Merchant { get; set; }
        public string? SourceAccount { get; set; }
        public string? TargetAccount { get; set; }
        public string? SourceBank { get; set; }
        public string? TargetBank { get; set; }
        public decimal? Fees { get; set; }
        public decimal? Tax { get; set; }
        public string? ReferenceId { get; set; }
        public double AmountConfidence { get; set; }
        public double DateConfidence { get; set; }
        public double MerchantConfidence { get; set; }
        public string? Reasoning { get; set; }
        public bool HasAmbiguities { get; set; }
    }
}
