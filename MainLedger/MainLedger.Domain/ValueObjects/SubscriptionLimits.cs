using MainLedger.Domain.Entities;

namespace MainLedger.Domain.ValueObjects;

/// <summary>
/// Value object representing subscription limits and capabilities.
/// </summary>
public class SubscriptionLimits
{
    public int MonthlyEmailLimit { get; }
    public int MaxGmailAccounts { get; }
    public int MaxApiKeys { get; }
    public int HistoryRetentionDays { get; }
    public bool CanExport { get; }
    public bool CanUseWorkflowAutomation { get; }
    public bool CanUseWebhooks { get; }
    public int MaxWebhooks { get; }
    public bool CanUseBulkOperations { get; }

    public SubscriptionLimits(
        int monthlyEmailLimit,
        int maxGmailAccounts,
        int maxApiKeys,
        int historyRetentionDays,
        bool canExport,
        bool canUseWorkflowAutomation,
        bool canUseWebhooks,
        int maxWebhooks,
        bool canUseBulkOperations
    )
    {
        MonthlyEmailLimit = monthlyEmailLimit;
        MaxGmailAccounts = maxGmailAccounts;
        MaxApiKeys = maxApiKeys;
        HistoryRetentionDays = historyRetentionDays;
        CanExport = canExport;
        CanUseWorkflowAutomation = canUseWorkflowAutomation;
        CanUseWebhooks = canUseWebhooks;
        MaxWebhooks = maxWebhooks;
        CanUseBulkOperations = canUseBulkOperations;
    }

    public static SubscriptionLimits FromPlan(SubscriptionPlan plan)
    {
        return new SubscriptionLimits(
            plan.MonthlyEmailLimit,
            plan.MaxGmailAccounts,
            plan.MaxApiKeys,
            plan.HistoryRetentionDays,
            plan.CanExport,
            plan.CanUseWorkflowAutomation,
            plan.CanUseWebhooks,
            plan.MaxWebhooks,
            plan.CanUseBulkOperations
        );
    }
}
