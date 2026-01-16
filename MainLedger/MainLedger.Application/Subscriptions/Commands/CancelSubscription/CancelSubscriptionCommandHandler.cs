using MainLedger.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.Subscriptions.Commands.CancelSubscription;

public class CancelSubscriptionCommandHandler : IRequestHandler<CancelSubscriptionCommand>
{
    private readonly IUserSubscriptionRepository _subscriptionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CancelSubscriptionCommandHandler> _logger;

    public CancelSubscriptionCommandHandler(
        IUserSubscriptionRepository subscriptionRepository,
        IUnitOfWork unitOfWork,
        ILogger<CancelSubscriptionCommandHandler> logger
    )
    {
        _subscriptionRepository = subscriptionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(CancelSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionRepository.GetByUserIdAsync(
            request.UserId,
            cancellationToken
        );

        if (subscription == null)
        {
            throw new KeyNotFoundException($"No subscription found for user {request.UserId}.");
        }

        subscription.Cancel();
        _subscriptionRepository.Update(subscription);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "User {UserId} cancelled subscription. Reason: {Reason}. Subscription will end on {EndDate}",
            request.UserId,
            request.Reason,
            subscription.EndDate
        );
    }
}
