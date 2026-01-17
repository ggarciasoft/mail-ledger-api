using MainLedger.Contracts.Subscriptions;
using MainLedger.Domain.Repositories;
using MediatR;

namespace MainLedger.Application.Subscriptions.Queries.GetUserSubscription;

public class GetUserSubscriptionQueryHandler
    : IRequestHandler<GetUserSubscriptionQuery, UserSubscriptionDto?>
{
    private readonly IUserSubscriptionRepository _subscriptionRepository;

    public GetUserSubscriptionQueryHandler(IUserSubscriptionRepository subscriptionRepository)
    {
        _subscriptionRepository = subscriptionRepository;
    }

    public async Task<UserSubscriptionDto?> Handle(
        GetUserSubscriptionQuery request,
        CancellationToken cancellationToken
    )
    {
        var subscription = await _subscriptionRepository.GetByUserIdAsync(
            request.UserId,
            cancellationToken
        );

        if (subscription == null)
        {
            return null;
        }

        return new UserSubscriptionDto(
            subscription.Id,
            new SubscriptionPlanDto(
                subscription.SubscriptionPlan.Id,
                subscription.SubscriptionPlan.Name,
                subscription.SubscriptionPlan.Description,
                subscription.SubscriptionPlan.MonthlyPrice,
                subscription.SubscriptionPlan.MonthlyEmailLimit,
                subscription.SubscriptionPlan.MaxEmailAccounts,
                subscription.SubscriptionPlan.MaxApiKeys,
                subscription.SubscriptionPlan.HistoryRetentionDays,
                subscription.SubscriptionPlan.CanExport,
                subscription.SubscriptionPlan.CanUseWorkflowAutomation,
                subscription.SubscriptionPlan.CanUseWebhooks,
                subscription.SubscriptionPlan.MaxWebhooks,
                subscription.SubscriptionPlan.CanUseBulkOperations,
                subscription.SubscriptionPlan.IsActive
            ),
            subscription.StartDate,
            subscription.EndDate,
            subscription.Status.ToString(),
            subscription.EmailsProcessedThisMonth,
            subscription.CurrentPeriodStart,
            subscription.CurrentPeriodEnd
        );
    }
}
