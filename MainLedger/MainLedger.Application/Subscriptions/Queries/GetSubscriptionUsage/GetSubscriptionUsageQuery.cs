using MainLedger.Contracts.Subscriptions;
using MediatR;

namespace MainLedger.Application.Subscriptions.Queries.GetSubscriptionUsage;

/// <summary>
/// Query to get subscription usage statistics for a user.
/// </summary>
public record GetSubscriptionUsageQuery(Guid UserId) : IRequest<SubscriptionUsageDto>;
