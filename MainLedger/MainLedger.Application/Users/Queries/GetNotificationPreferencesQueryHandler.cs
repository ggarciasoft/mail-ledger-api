using MainLedger.Contracts.Users;
using MainLedger.Domain.Repositories;
using MediatR;

namespace MainLedger.Application.Users.Queries;

public class GetNotificationPreferencesQueryHandler
    : IRequestHandler<GetNotificationPreferencesQuery, NotificationPreferencesDto>
{
    private readonly IUserRepository _userRepository;

    public GetNotificationPreferencesQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<NotificationPreferencesDto> Handle(
        GetNotificationPreferencesQuery request,
        CancellationToken cancellationToken
    )
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user == null)
        {
            throw new InvalidOperationException($"User {request.UserId} not found");
        }

        return new NotificationPreferencesDto(
            user.EmailNotificationsEnabled,
            user.NotifyOnEmailSync,
            user.NotifyOnClassification,
            user.NotifyOnExtraction
        );
    }
}
