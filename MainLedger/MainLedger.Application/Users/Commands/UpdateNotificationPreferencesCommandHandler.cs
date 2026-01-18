using MainLedger.Contracts.Users;
using MainLedger.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.Users.Commands;

public class UpdateNotificationPreferencesCommandHandler
    : IRequestHandler<UpdateNotificationPreferencesCommand, NotificationPreferencesDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateNotificationPreferencesCommandHandler> _logger;

    public UpdateNotificationPreferencesCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateNotificationPreferencesCommandHandler> logger
    )
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<NotificationPreferencesDto> Handle(
        UpdateNotificationPreferencesCommand request,
        CancellationToken cancellationToken
    )
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user == null)
        {
            throw new InvalidOperationException($"User {request.UserId} not found");
        }

        user.UpdateNotificationPreferences(
            request.EmailNotificationsEnabled,
            request.NotifyOnEmailSync,
            request.NotifyOnClassification,
            request.NotifyOnExtraction
        );

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated notification preferences for user {UserId}",
            request.UserId
        );

        return new NotificationPreferencesDto(
            user.EmailNotificationsEnabled,
            user.NotifyOnEmailSync,
            user.NotifyOnClassification,
            user.NotifyOnExtraction
        );
    }
}
