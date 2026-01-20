namespace MainLedger.Contracts.ExtractionCandidates;

/// <summary>
/// Request to confirm an extraction candidate with optional overrides.
/// </summary>
public record ConfirmCandidateRequest
{
    /// <summary>
    /// Optional merchant name override. If provided, this will be used instead of the candidate's merchant.
    /// </summary>
    public string? Merchant { get; init; }
}
