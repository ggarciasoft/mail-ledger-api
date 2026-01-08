namespace MainLedger.Contracts.FinancialRecords;

/// <summary>
/// Financial record list item DTO for paginated lists.
/// </summary>
public class FinancialRecordListItemDto
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string? Merchant { get; init; }
    public DateTime TransactionDate { get; init; }
    public string? SourceAccount { get; init; }
    public string? SourceBank { get; init; }
    public string Direction { get; init; } = string.Empty;
    public DateTime ConfirmedAt { get; init; }
}
