namespace MainLedger.Domain.Enums;

/// <summary>
/// Type of background processing job.
/// </summary>
public enum JobType
{
    EmailSync,
    Classification,
    Extraction,
}
