namespace MainLedger.Contracts.ExtractionCandidates;

/// <summary>
/// Request to reject an extraction candidate.
/// </summary>
public class RejectExtractionRequest
{
    public string Reason { get; init; } = string.Empty;
}
