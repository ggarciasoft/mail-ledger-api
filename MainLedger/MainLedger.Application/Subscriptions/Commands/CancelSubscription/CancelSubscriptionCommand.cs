using MediatR;

namespace MainLedger.Application.Subscriptions.Commands.CancelSubscription;

/// <summary>
/// Command to cancel a user's subscription.
/// </summary>
public record CancelSubscriptionCommand(Guid UserId, string Reason) : IRequest;
