using MainLedger.Contracts.Subscriptions;
using MediatR;

namespace MainLedger.Application.Subscriptions.Queries.GetSubscriptionPlans;

/// <summary>
/// Query to get all active subscription plans.
/// </summary>
public record GetSubscriptionPlansQuery : IRequest<List<SubscriptionPlanDto>>;
