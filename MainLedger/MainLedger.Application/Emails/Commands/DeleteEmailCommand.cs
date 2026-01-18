using MediatR;

namespace MainLedger.Application.Emails.Commands;

/// <summary>
/// Command to delete a single email.
/// </summary>
public record DeleteEmailCommand(Guid UserId, Guid EmailId) : IRequest<bool>;
