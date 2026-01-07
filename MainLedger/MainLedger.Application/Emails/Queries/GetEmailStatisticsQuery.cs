using MainLedger.Contracts.Emails;
using MediatR;

namespace MainLedger.Application.Emails.Queries;

/// <summary>
/// Query to get email statistics for a user.
/// </summary>
public record GetEmailStatisticsQuery(Guid UserId) : IRequest<EmailStatisticsDto>;
