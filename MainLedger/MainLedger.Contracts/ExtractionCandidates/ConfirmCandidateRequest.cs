/// <summary>
/// Request to confirm an extraction candidate with optional overrides.
/// </summary>
public record ConfirmCandidateRequest
{
    /// <summary>
    /// Optional merchant name override. If provided, this will be used instead of the candidate's merchant.
    /// </summary>
    public string? Merchant { get; init; }

    /// <summary>
    /// Optional category name. If provided, the category will be auto-created if it doesn't exist.
    /// </summary>
    public string? Category { get; init; }
}
