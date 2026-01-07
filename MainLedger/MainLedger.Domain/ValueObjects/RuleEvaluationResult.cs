using MainLedger.Domain.Common;
using MainLedger.Domain.Enums;

namespace MainLedger.Domain.ValueObjects;

/// <summary>
/// Result of rules engine evaluation for an email.
/// Contains the processing directive and reasoning for auditability.
/// </summary>
public sealed class RuleEvaluationResult : ValueObject
{
    public ProcessingDirective Directive { get; }
    public string Reason { get; }
    public Guid? MatchedRuleId { get; }
    public bool ShouldProcess => Directive != ProcessingDirective.Ignore;

    private RuleEvaluationResult(
        ProcessingDirective directive,
        string reason,
        Guid? matchedRuleId = null)
    {
        Directive = directive;
        Reason = reason;
        MatchedRuleId = matchedRuleId;
    }

    /// <summary>
    /// Creates a result indicating the email should be ignored.
    /// </summary>
    public static RuleEvaluationResult Ignore(string reason)
    {
        return new RuleEvaluationResult(ProcessingDirective.Ignore, reason);
    }

    /// <summary>
    /// Creates a result indicating the email should be classified.
    /// </summary>
    public static RuleEvaluationResult Classify(string reason, Guid? ruleId = null)
    {
        return new RuleEvaluationResult(ProcessingDirective.Classify, reason, ruleId);
    }

    /// <summary>
    /// Creates a result indicating the email should be extracted directly.
    /// </summary>
    public static RuleEvaluationResult Extract(string reason, Guid ruleId)
    {
        return new RuleEvaluationResult(ProcessingDirective.Extract, reason, ruleId);
    }

    /// <summary>
    /// Creates a result indicating the email needs manual review.
    /// </summary>
    public static RuleEvaluationResult FlagForReview(string reason)
    {
        return new RuleEvaluationResult(ProcessingDirective.FlagForReview, reason);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Directive;
        yield return Reason;
        yield return MatchedRuleId;
    }
}

