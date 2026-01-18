using MediatR;

namespace MainLedger.Application.Emails.Commands;

/// <summary>
/// Command to bulk delete emails.
/// </summary>
public record BulkDeleteEmailsCommand(Guid UserId, List<Guid> EmailIds)
    : IRequest<BulkDeleteResult>;

public record BulkDeleteResult
{
    public int TotalRequested { get; init; }
    public int Succeeded { get; init; }
    public int Failed { get; init; }
    public List<BulkDeleteError> Errors { get; init; } = new();
}

public record BulkDeleteError
{
    public Guid EmailId { get; init; }
    public string Error { get; init; } = string.Empty;
}
