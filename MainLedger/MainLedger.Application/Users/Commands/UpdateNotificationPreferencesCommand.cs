using MainLedger.Contracts.Users;
using MediatR;

namespace MainLedger.Application.Users.Commands;

public record UpdateNotificationPreferencesCommand(
    Guid UserId,
    bool EmailNotificationsEnabled,
    bool NotifyOnEmailSync,
    bool NotifyOnClassification,
    bool NotifyOnExtraction
) : IRequest<NotificationPreferencesDto>;
