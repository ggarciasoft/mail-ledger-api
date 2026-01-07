namespace MainLedger.Domain.Enums;

/// <summary>
/// Directive for what action should be taken on an email after rules evaluation.
/// Determines the next step in the processing pipeline.
/// </summary>
public enum ProcessingDirective
{
    /// <summary>
    /// Email should be ignored and not processed further.
    /// </summary>
    Ignore = 0,

    /// <summary>
    /// Email should be classified to determine if it's financial.
    /// </summary>
    Classify = 1,

    /// <summary>
    /// Email should be sent directly to extraction (skipping classification).
    /// Used when rules determine with high confidence that email is financial.
    /// </summary>
    Extract = 2,

    /// <summary>
    /// Email requires manual review before processing.
    /// </summary>
    FlagForReview = 3
}

