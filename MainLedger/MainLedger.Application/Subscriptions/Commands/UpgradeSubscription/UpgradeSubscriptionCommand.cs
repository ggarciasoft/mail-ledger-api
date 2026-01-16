using MediatR;

namespace MainLedger.Application.Subscriptions.Commands.UpgradeSubscription;

/// <summary>
/// Command to upgrade a user's subscription to a new plan.
/// </summary>
public record UpgradeSubscriptionCommand(Guid UserId, Guid NewPlanId) : IRequest;
