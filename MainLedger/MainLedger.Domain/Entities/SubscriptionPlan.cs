using MainLedger.Domain.Common;

namespace MainLedger.Domain.Entities;

/// <summary>
/// Represents a subscription plan with its features and limits.
/// </summary>
public class SubscriptionPlan : Entity
{
    // Predefined plan IDs for seeding
    public static readonly Guid FreePlanId = new("00000000-0000-0000-0000-000000000001");
    public static readonly Guid BasicPlanId = new("00000000-0000-0000-0000-000000000002");
    public static readonly Guid ProPlanId = new("00000000-0000-0000-0000-000000000003");
    public static readonly Guid EnterprisePlanId = new("00000000-0000-0000-0000-000000000004");

    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal MonthlyPrice { get; private set; }

    // Email processing limits
    public int MonthlyEmailLimit { get; private set; }

    // Connection limits
    public int MaxEmailAccounts { get; private set; }
    public int MaxApiKeys { get; private set; }

    // Data retention
    public int HistoryRetentionDays { get; private set; }

    // Feature flags
    public bool CanExport { get; private set; }
    public bool CanUseWorkflowAutomation { get; private set; }
    public bool CanUseWebhooks { get; private set; }
    public int MaxWebhooks { get; private set; }
    public bool CanUseBulkOperations { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public bool IsActive { get; private set; }

    // Navigation properties
    public ICollection<UserSubscription> UserSubscriptions { get; private set; } =
        new List<UserSubscription>();

    private SubscriptionPlan() { } // EF Core

    public SubscriptionPlan(
        Guid id,
        string name,
        string description,
        decimal monthlyPrice,
        int monthlyEmailLimit,
        int maxEmailAccounts,
        int maxApiKeys,
        int historyRetentionDays,
        bool canExport,
        bool canUseWorkflowAutomation,
        bool canUseWebhooks,
        int maxWebhooks,
        bool canUseBulkOperations
    )
    {
        Id = id;
        Name = name;
        Description = description;
        MonthlyPrice = monthlyPrice;
        MonthlyEmailLimit = monthlyEmailLimit;
        MaxEmailAccounts = maxEmailAccounts;
        MaxApiKeys = maxApiKeys;
        HistoryRetentionDays = historyRetentionDays;
        CanExport = canExport;
        CanUseWorkflowAutomation = canUseWorkflowAutomation;
        CanUseWebhooks = canUseWebhooks;
        MaxWebhooks = maxWebhooks;
        CanUseBulkOperations = canUseBulkOperations;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void UpdatePricing(decimal newPrice)
    {
        MonthlyPrice = newPrice;
    }
}
