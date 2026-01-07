using MainLedger.Domain.Entities;
using MainLedger.Domain.ValueObjects;

namespace MainLedger.Application.Common.Interfaces;

/// <summary>
/// Rules Engine service that determines what should happen with an ingested email.
/// Applies deterministic business rules without calling AI models.
/// </summary>
public interface IRulesEngine
{
    /// <summary>
    /// Evaluates an email against user-defined rules and system constraints.
    /// Returns a directive for what action should be taken next.
    /// </summary>
    /// <param name="email">The email to evaluate.</param>
    /// <param name="rules">Active rules for the user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing processing directive and reasoning.</returns>
    Task<RuleEvaluationResult> EvaluateAsync(
        EmailMessage email,
        List<Rule> rules,
        CancellationToken cancellationToken = default);
}

