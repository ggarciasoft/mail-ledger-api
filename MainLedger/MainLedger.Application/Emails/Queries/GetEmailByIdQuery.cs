using MainLedger.Contracts.Emails;
using MediatR;

namespace MainLedger.Application.Emails.Queries;

/// <summary>
/// Query to get a single email by ID.
/// </summary>
public record GetEmailByIdQuery(Guid EmailId, Guid UserId) : IRequest<EmailDto?>;
