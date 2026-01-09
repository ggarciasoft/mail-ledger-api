using MainLedger.Contracts.Gmail;
using MediatR;

namespace MainLedger.Application.Gmail.Queries;

/// <summary>
/// Query to get Gmail connection status for a user.
/// </summary>
public record GetGmailConnectionStatusQuery(Guid UserId) : IRequest<GmailConnectionStatusDto>;
