using System.Text;
using System.Text.Json;
using MainLedger.Application.Common.Interfaces;
using MainLedger.Application.Common.Models;
using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Settings;
using MainLedger.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace MainLedger.Integrations.Services;

/// <summary>
/// Classification service using OpenAI GPT models.
/// </summary>
public class OpenAIClassificationService : IClassificationService
{
    private readonly OpenAISettings _settings;
    private readonly ILogger<OpenAIClassificationService> _logger;
    private readonly ChatClient _chatClient;

    public OpenAIClassificationService(
        IOptions<OpenAISettings> settings,
        ILogger<OpenAIClassificationService> logger
    )
    {
        _settings = settings.Value;
        _logger = logger;
        _chatClient = new ChatClient(_settings.Model, _settings.ApiKey);
    }

    public async Task<ClassificationResult> ClassifyEmailAsync(
        EmailMessage email,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            // Clean and truncate body for cost control
            var cleanedBody = CleanHtml(email.BodyText);
            var body = TruncateBody(cleanedBody, _settings.MaxBodyLength);

            // Build prompt
            var prompt = BuildClassificationPrompt(email.Subject, email.From.Value, body);

            _logger.LogDebug(
                "Classifying email {MessageId} from {Sender}",
                email.MessageId,
                email.From.Value
            );

            // Call OpenAI API
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(GetSystemPrompt()),
                new UserChatMessage(prompt),
            };

            var options = new ChatCompletionOptions
            {
                MaxOutputTokenCount = _settings.MaxTokens,
                Temperature = (float)_settings.Temperature,
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
            };

            var response = await _chatClient.CompleteChatAsync(
                messages,
                options,
                cancellationToken
            );

            // Parse response
            var result = ParseClassificationResponse(response.Value.Content[0].Text);

            _logger.LogInformation(
                "Email {MessageId} classified: IsFinancial={IsFinancial}, Category={Category}, Confidence={Confidence}",
                email.MessageId,
                result.IsFinancial,
                result.Category,
                result.Confidence.Value
            );

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to classify email {MessageId}", email.MessageId);

            // Return safe default on error
            return new ClassificationResult
            {
                IsFinancial = false,
                Category = null,
                Confidence = Confidence.Create(0.0),
                Reasoning = $"Classification failed: {ex.Message}",
            };
        }
    }

    private string CleanHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var cleaned = html;

        // Remove style tags and their content (case-insensitive, multiline)
        cleaned = System.Text.RegularExpressions.Regex.Replace(
            cleaned,
            @"<style[^>]*>.*?</style>",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);

        // Remove script tags and their content
        cleaned = System.Text.RegularExpressions.Regex.Replace(
            cleaned,
            @"<script[^>]*>.*?</script>",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);

        // Remove style attributes with double quotes
        cleaned = System.Text.RegularExpressions.Regex.Replace(
            cleaned,
            @"\s+style\s*=\s*""[^""]*""",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Remove style attributes with single quotes
        cleaned = System.Text.RegularExpressions.Regex.Replace(
            cleaned,
            @"\s+style\s*=\s*'[^']*'",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Remove class attributes (optional - reduces noise)
        cleaned = System.Text.RegularExpressions.Regex.Replace(
            cleaned,
            @"\s+class\s*=\s*""[^""]*""",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        cleaned = System.Text.RegularExpressions.Regex.Replace(
            cleaned,
            @"\s+class\s*=\s*'[^']*'",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return cleaned;
    }

    private string GetSystemPrompt()
    {
        return @"You are an expert financial email classifier. Your task is to analyze emails and determine:
1. Whether the email is financial-related (payments, transfers, receipts, invoices, statements, etc.)
2. If financial, what category it belongs to
3. Your confidence level (0.0 to 1.0)

Respond ONLY with valid JSON in this exact format:
{
  ""isFinancial"": true/false,
  ""category"": ""Payment"" | ""Transfer"" | ""Receipt"" | ""Invoice"" | ""Statement"" | ""Authorization"" | ""Refund"" | ""Other"" | null,
  ""confidence"": 0.0-1.0,
  ""reasoning"": ""brief explanation""
}

Categories:
- Payment: Money sent or received for goods/services
- Transfer: Money moved between accounts
- Receipt: Confirmation of purchase or payment
- Invoice: Bill or request for payment
- Statement: Account balance or transaction summary
- Authorization: Pre-authorization or pending charge
- Refund: Money returned or reversed
- Other: Financial but doesn't fit above categories

If not financial, set category to null.";
    }

    private string BuildClassificationPrompt(string subject, string from, string body)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Classify this email:");
        sb.AppendLine();
        sb.AppendLine($"From: {from}");
        sb.AppendLine($"Subject: {subject}");
        sb.AppendLine();
        sb.AppendLine("Body:");
        sb.AppendLine(body);

        return sb.ToString();
    }

    private ClassificationResult ParseClassificationResponse(string jsonResponse)
    {
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var response = JsonSerializer.Deserialize<ClassificationResponse>(
                jsonResponse,
                options
            );

            if (response == null)
            {
                throw new InvalidOperationException(
                    "Failed to deserialize classification response"
                );
            }

            EmailCategory? category = null;
            if (response.IsFinancial && !string.IsNullOrWhiteSpace(response.Category))
            {
                if (Enum.TryParse<EmailCategory>(response.Category, true, out var parsedCategory))
                {
                    category = parsedCategory;
                }
            }

            return new ClassificationResult
            {
                IsFinancial = response.IsFinancial,
                Category = category,
                Confidence = Confidence.Create(Math.Clamp(response.Confidence, 0.0, 1.0)),
                Reasoning = response.Reasoning ?? string.Empty,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to parse classification response: {Response}",
                jsonResponse
            );

            // Return low-confidence non-financial result
            return new ClassificationResult
            {
                IsFinancial = false,
                Category = null,
                Confidence = Confidence.Create(0.0),
                Reasoning = $"Parse error: {ex.Message}",
            };
        }
    }

    private string TruncateBody(string body, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(body))
            return string.Empty;

        if (body.Length <= maxLength)
            return body;

        // Truncate and add indicator
        return body.Substring(0, maxLength) + "\n\n[... truncated for length ...]";
    }

    // Internal model for JSON deserialization
    private class ClassificationResponse
    {
        public bool IsFinancial { get; set; }
        public string? Category { get; set; }
        public double Confidence { get; set; }
        public string? Reasoning { get; set; }
    }
}
