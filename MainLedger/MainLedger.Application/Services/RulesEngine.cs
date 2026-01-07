using MainLedger.Application.Common.Interfaces;
using MainLedger.Domain.Entities;
using MainLedger.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.Services;

/// <summary>
/// Implementation of the Rules Engine.
/// Evaluates emails against deterministic rules to control processing flow.
/// </summary>
public class RulesEngine : IRulesEngine
{
    private readonly ILogger<RulesEngine> _logger;

    // System-level rules (hard-coded for reliability)
    private static readonly HashSet<string> SystemBlockedDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "noreply@",
        "no-reply@",
        "donotreply@",
        "notifications@",
        "newsletter@"
    };

    private static readonly HashSet<string> FinancialKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "payment", "transaction", "transfer", "invoice", "receipt", 
        "paid", "charged", "refund", "balance", "statement",
        "deposit", "withdrawal", "credit", "debit", "purchase"
    };

    public RulesEngine(ILogger<RulesEngine> logger)
    {
        _logger = logger;
    }

    public Task<RuleEvaluationResult> EvaluateAsync(
        EmailMessage email,
        List<Rule> rules,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Apply system-level blocklist (hard-coded rules)
        if (IsSystemBlocked(email))
        {
            _logger.LogDebug(
                "Email {EmailId} from {Sender} blocked by system rules",
                email.MessageId, email.From.Value);

            return Task.FromResult(
                RuleEvaluationResult.Ignore("System blocked: sender is on blocklist"));
        }

        // Step 2: Check if ANY user rule matches (allowlist pattern)
        var matchedRule = rules
            .Where(r => r.IsActive)
            .OrderBy(r => r.Priority)
            .FirstOrDefault(r => r.Matches(email));

        if (matchedRule != null)
        {
            _logger.LogInformation(
                "Email {EmailId} matched rule {RuleId} ({RuleName})",
                email.MessageId, matchedRule.Id, matchedRule.Name);

            // High-confidence financial emails can skip classification
            if (IsHighConfidenceFinancial(email, matchedRule))
            {
                return Task.FromResult(
                    RuleEvaluationResult.Extract(
                        $"High confidence financial email matching rule: {matchedRule.Name}",
                        matchedRule.Id));
            }

            // Default: classify to verify
            return Task.FromResult(
                RuleEvaluationResult.Classify(
                    $"Matched rule: {matchedRule.Name}",
                    matchedRule.Id));
        }

        // Step 3: Apply heuristic checks for potential financial content
        if (HasFinancialKeywords(email))
        {
            _logger.LogDebug(
                "Email {EmailId} contains financial keywords, routing to classification",
                email.MessageId);

            return Task.FromResult(
                RuleEvaluationResult.Classify(
                    "Contains financial keywords, needs classification"));
        }

        // Step 4: No rules matched and no financial indicators - ignore
        _logger.LogDebug(
            "Email {EmailId} did not match any rules or heuristics, ignoring",
            email.MessageId);

        return Task.FromResult(
            RuleEvaluationResult.Ignore(
                "No matching rules and no financial indicators detected"));
    }

    /// <summary>
    /// Checks if sender is on system blocklist.
    /// </summary>
    private bool IsSystemBlocked(EmailMessage email)
    {
        var senderEmail = email.From.Value.ToLowerInvariant();
        
        // Check if sender starts with blocked patterns
        return SystemBlockedDomains.Any(blocked => 
            senderEmail.StartsWith(blocked, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Determines if email has high confidence of being financial.
    /// Used to skip classification and go straight to extraction.
    /// </summary>
    private bool IsHighConfidenceFinancial(EmailMessage email, Rule matchedRule)
    {
        // High confidence if:
        // 1. Rule has explicit sender pattern (known financial institution)
        // 2. Email contains multiple financial keywords
        // 3. Subject contains financial indicators

        var hasExplicitSender = !string.IsNullOrWhiteSpace(matchedRule.SenderPattern);
        var hasFinancialKeywords = HasMultipleFinancialKeywords(email);
        var hasFinancialSubject = ContainsFinancialKeywords(email.Subject);

        return hasExplicitSender && (hasFinancialKeywords || hasFinancialSubject);
    }

    /// <summary>
    /// Checks if email has at least one financial keyword.
    /// </summary>
    private bool HasFinancialKeywords(EmailMessage email)
    {
        var contentToSearch = $"{email.Subject} {email.BodyText}".ToLowerInvariant();
        return FinancialKeywords.Any(keyword => 
            contentToSearch.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if email has multiple financial keywords (higher confidence).
    /// </summary>
    private bool HasMultipleFinancialKeywords(EmailMessage email)
    {
        var contentToSearch = $"{email.Subject} {email.BodyText}".ToLowerInvariant();
        var matchCount = FinancialKeywords.Count(keyword => 
            contentToSearch.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        
        return matchCount >= 2;
    }

    /// <summary>
    /// Checks if text contains financial keywords.
    /// </summary>
    private bool ContainsFinancialKeywords(string text)
    {
        var lowerText = text.ToLowerInvariant();
        return FinancialKeywords.Any(keyword => 
            lowerText.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}

