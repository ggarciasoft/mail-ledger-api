namespace MainLedger.Domain.Enums;

/// <summary>
/// Represents the processing status of an email message in the pipeline.
/// </summary>
public enum EmailProcessingStatus
{
    /// <summary>
    /// Email has been ingested but not yet classified.
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Email has been classified by AI.
    /// </summary>
    Classified = 1,
    
    /// <summary>
    /// Financial data has been extracted from the email.
    /// </summary>
    Extracted = 2,
    
    /// <summary>
    /// Processing failed at some stage.
    /// </summary>
    Failed = 3
}
