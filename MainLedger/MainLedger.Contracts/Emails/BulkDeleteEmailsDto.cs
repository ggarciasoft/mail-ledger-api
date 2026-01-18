namespace MainLedger.Contracts.Emails;

/// <summary>
/// Request to bulk delete emails.
/// </summary>
public record BulkDeleteEmailsRequest(List<Guid> EmailIds);

/// <summary>
/// Response for bulk delete operation.
/// </summary>
public record BulkDeleteEmailsResponse(
    int TotalRequested,
    int Succeeded,
    int Failed,
    List<BulkDeleteEmailError> Errors
);

/// <summary>
/// Error details for a failed email deletion.
/// </summary>
public record BulkDeleteEmailError(Guid EmailId, string Error);
