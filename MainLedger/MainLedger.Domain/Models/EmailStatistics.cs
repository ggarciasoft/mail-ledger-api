namespace MainLedger.Domain.Models;

/// <summary>
/// Email statistics model.
/// </summary>
public class EmailStatistics
{
    public int TotalEmails { get; init; }
    public int Pending { get; init; }
    public int Classified { get; init; }
    public int Extracted { get; init; }
    public int Failed { get; init; }
    public int FinancialEmails { get; init; }
    public int NonFinancialEmails { get; init; }
}
