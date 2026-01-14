namespace MainLedger.Domain.Enums;

/// <summary>
/// Defines how the workflow automation runs.
/// </summary>
public enum WorkflowMode
{
    /// <summary>
    /// No automation - user triggers jobs manually.
    /// </summary>
    Manual = 0,

    /// <summary>
    /// Each job type runs independently on its own schedule.
    /// </summary>
    Separate = 1,

    /// <summary>
    /// Jobs run sequentially in a pipeline: Sync → Classification → Extraction.
    /// </summary>
    Sequential = 2,
}
