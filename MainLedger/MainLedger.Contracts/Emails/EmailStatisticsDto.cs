namespace MainLedger.Contracts.Emails;

/// <summary>
/// Email statistics DTO.
/// </summary>
public class EmailStatisticsDto
{
    public int TotalEmails { get; init; }
    public int Pending { get; init; }
    public int Classified { get; init; }
    public int Extracted { get; init; }
    public int Failed { get; init; }
    public int FinancialEmails { get; init; }
    public int NonFinancialEmails { get; init; }
    public DateTime? LastSyncAt { get; init; }
}
