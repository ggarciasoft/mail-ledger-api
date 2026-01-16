using MainLedger.Domain.Entities;
using MainLedger.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.Subscriptions.Commands.UpgradeSubscription;

public class UpgradeSubscriptionCommandHandler : IRequestHandler<UpgradeSubscriptionCommand>
{
    private readonly IUserSubscriptionRepository _subscriptionRepository;
    private readonly ISubscriptionPlanRepository _planRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpgradeSubscriptionCommandHandler> _logger;

    public UpgradeSubscriptionCommandHandler(
        IUserSubscriptionRepository subscriptionRepository,
        ISubscriptionPlanRepository planRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpgradeSubscriptionCommandHandler> logger
    )
    {
        _subscriptionRepository = subscriptionRepository;
        _planRepository = planRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(
        UpgradeSubscriptionCommand request,
        CancellationToken cancellationToken
    )
    {
        // Verify new plan exists
        var newPlan = await _planRepository.GetByIdAsync(request.NewPlanId, cancellationToken);
        if (newPlan == null)
        {
            throw new KeyNotFoundException($"Subscription plan {request.NewPlanId} not found.");
        }

        if (!newPlan.IsActive)
        {
            throw new InvalidOperationException("Cannot upgrade to an inactive plan.");
        }

        // Get or create user subscription
        var subscription = await _subscriptionRepository.GetByUserIdAsync(
            request.UserId,
            cancellationToken
        );

        if (subscription == null)
        {
            // Create new subscription with the selected plan
            subscription = new UserSubscription(request.UserId, request.NewPlanId);
            _subscriptionRepository.Add(subscription);
        }
        else
        {
            // Upgrade existing subscription
            subscription.Upgrade(request.NewPlanId);
            _subscriptionRepository.Update(subscription);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "User {UserId} upgraded subscription to plan {PlanId} ({PlanName})",
            request.UserId,
            newPlan.Id,
            newPlan.Name
        );
    }
}
