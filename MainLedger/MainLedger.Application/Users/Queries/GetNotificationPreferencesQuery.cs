using MainLedger.Contracts.Users;
using MediatR;

namespace MainLedger.Application.Users.Queries;

public record GetNotificationPreferencesQuery(Guid UserId) : IRequest<NotificationPreferencesDto>;
