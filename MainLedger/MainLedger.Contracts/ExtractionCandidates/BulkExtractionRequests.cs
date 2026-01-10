namespace MainLedger.Contracts.ExtractionCandidates;

/// <summary>
/// Request to bulk confirm multiple extraction candidates.
/// </summary>
public class BulkConfirmRequest
{
    public List<Guid> CandidateIds { get; init; } = new();
}

/// <summary>
/// Request to bulk reject multiple extraction candidates.
/// </summary>
public class BulkRejectRequest
{
    public List<Guid> CandidateIds { get; init; } = new();
    public string? Reason { get; init; }
}

/// <summary>
/// Response for bulk operations on extraction candidates.
/// </summary>
public class BulkOperationResponse
{
    public int TotalRequested { get; init; }
    public int Succeeded { get; init; }
    public int Failed { get; init; }
    public List<BulkOperationError> Errors { get; init; } = new();
}

/// <summary>
/// Error details for a failed bulk operation item.
/// </summary>
public class BulkOperationError
{
    public Guid CandidateId { get; init; }
    public string Error { get; init; } = string.Empty;
}
