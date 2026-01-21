namespace MainLedger.Contracts.Subscriptions;

/// <summary>
/// DTO for subscription plan information.
/// </summary>
public record SubscriptionPlanDto(
    Guid Id,
    string Name,
    string Description,
    decimal MonthlyPrice,
    int ClassificationLimit,
    int ExtractionLimit,
    int MaxEmailAccounts,
    int MaxApiKeys,
    int HistoryRetentionDays,
    bool CanExport,
    bool CanUseWorkflowAutomation,
    bool CanUseWebhooks,
    int MaxWebhooks,
    bool CanUseBulkOperations,
    bool IsActive
);

/// <summary>
/// DTO for user subscription information.
/// </summary>
public record UserSubscriptionDto(
    Guid Id,
    SubscriptionPlanDto SubscriptionPlan,
    DateTime StartDate,
    DateTime? EndDate,
    string Status,
    int EmailsClassifiedThisMonth,
    int EmailsExtractedThisMonth,
    DateTime CurrentPeriodStart,
    DateTime CurrentPeriodEnd
);

/// <summary>
/// DTO for subscription usage statistics.
/// </summary>
public record SubscriptionUsageDto(
    int EmailsClassified,
    int EmailsExtracted,
    int ClassificationLimit,
    int ExtractionLimit,
    int EmailAccountsConnected,
    int EmailAccountsLimit,
    int ApiKeysCreated,
    int ApiKeysLimit
);

/// <summary>
/// Request to upgrade subscription.
/// </summary>
public record UpgradeSubscriptionRequest(Guid PlanId);

/// <summary>
/// Request to cancel subscription.
/// </summary>
public record CancelSubscriptionRequest(string Reason);
