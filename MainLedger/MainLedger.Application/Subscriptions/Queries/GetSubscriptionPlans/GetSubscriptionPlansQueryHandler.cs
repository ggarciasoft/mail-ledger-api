using MainLedger.Contracts.Subscriptions;
using MainLedger.Domain.Repositories;
using MediatR;

namespace MainLedger.Application.Subscriptions.Queries.GetSubscriptionPlans;

public class GetSubscriptionPlansQueryHandler
    : IRequestHandler<GetSubscriptionPlansQuery, List<SubscriptionPlanDto>>
{
    private readonly ISubscriptionPlanRepository _planRepository;

    public GetSubscriptionPlansQueryHandler(ISubscriptionPlanRepository planRepository)
    {
        _planRepository = planRepository;
    }

    public async Task<List<SubscriptionPlanDto>> Handle(
        GetSubscriptionPlansQuery request,
        CancellationToken cancellationToken
    )
    {
        var plans = await _planRepository.GetAllActiveAsync(cancellationToken);

        return plans
            .Select(p => new SubscriptionPlanDto(
                p.Id,
                p.Name,
                p.Description,
                p.MonthlyPrice,
                p.MonthlyEmailLimit,
                p.MaxGmailAccounts,
                p.MaxApiKeys,
                p.HistoryRetentionDays,
                p.CanExport,
                p.CanUseWorkflowAutomation,
                p.CanUseWebhooks,
                p.MaxWebhooks,
                p.CanUseBulkOperations,
                p.IsActive
            ))
            .ToList();
    }
}
