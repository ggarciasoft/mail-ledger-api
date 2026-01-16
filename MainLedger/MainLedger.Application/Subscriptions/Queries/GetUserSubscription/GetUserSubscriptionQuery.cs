using MainLedger.Contracts.Subscriptions;
using MediatR;

namespace MainLedger.Application.Subscriptions.Queries.GetUserSubscription;

/// <summary>
/// Query to get the current user's subscription.
/// </summary>
public record GetUserSubscriptionQuery(Guid UserId) : IRequest<UserSubscriptionDto?>;
