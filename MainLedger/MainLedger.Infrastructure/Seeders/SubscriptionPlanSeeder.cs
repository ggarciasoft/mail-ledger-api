using MainLedger.Domain.Entities;
using MainLedger.Infrastructure.Persistence;

namespace MainLedger.Infrastructure.Seeders;

/// <summary>
/// Seeds the database with predefined subscription plans.
/// </summary>
public static class SubscriptionPlanSeeder
{
    public static async Task SeedAsync(MailLedgerDbContext context)
    {
        // Check if plans already exist
        if (context.SubscriptionPlans.Any())
        {
            return;
        }

        var plans = new List<SubscriptionPlan>
        {
            // Free Tier
            new SubscriptionPlan(
                id: SubscriptionPlan.FreePlanId,
                name: "Free",
                description: "Perfect for trying out MailLedger",
                monthlyPrice: 0m,
                monthlyEmailLimit: 100,
                maxEmailAccounts: 1,
                maxApiKeys: 0,
                historyRetentionDays: 30,
                canExport: false,
                canUseWorkflowAutomation: false,
                canUseWebhooks: false,
                maxWebhooks: 0,
                canUseBulkOperations: false
            ),
            // Basic Tier
            new SubscriptionPlan(
                id: SubscriptionPlan.BasicPlanId,
                name: "Basic",
                description: "For individuals managing personal finances",
                monthlyPrice: 9m,
                monthlyEmailLimit: 1000,
                maxEmailAccounts: 2,
                maxApiKeys: 2,
                historyRetentionDays: 90,
                canExport: true,
                canUseWorkflowAutomation: true,
                canUseWebhooks: false,
                maxWebhooks: 0,
                canUseBulkOperations: true
            ),
            // Pro Tier
            new SubscriptionPlan(
                id: SubscriptionPlan.ProPlanId,
                name: "Pro",
                description: "For power users and small businesses",
                monthlyPrice: 29m,
                monthlyEmailLimit: 10000,
                maxEmailAccounts: 5,
                maxApiKeys: 10,
                historyRetentionDays: 365,
                canExport: true,
                canUseWorkflowAutomation: true,
                canUseWebhooks: true,
                maxWebhooks: 5,
                canUseBulkOperations: true
            ),
            // Enterprise Tier
            new SubscriptionPlan(
                id: SubscriptionPlan.EnterprisePlanId,
                name: "Enterprise",
                description: "For large organizations with custom needs",
                monthlyPrice: 99m,
                monthlyEmailLimit: int.MaxValue, // Unlimited
                maxEmailAccounts: int.MaxValue, // Unlimited
                maxApiKeys: int.MaxValue, // Unlimited
                historyRetentionDays: int.MaxValue, // Unlimited
                canExport: true,
                canUseWorkflowAutomation: true,
                canUseWebhooks: true,
                maxWebhooks: int.MaxValue, // Unlimited
                canUseBulkOperations: true
            ),
        };

        await context.SubscriptionPlans.AddRangeAsync(plans);
        await context.SaveChangesAsync();

        // Assign all existing users to Free plan
        var usersWithoutSubscription = context
            .Users.Where(u => !context.UserSubscriptions.Any(s => s.UserId == u.Id))
            .ToList();

        var userSubscriptions = usersWithoutSubscription
            .Select(user => new UserSubscription(user.Id, SubscriptionPlan.FreePlanId))
            .ToList();

        if (userSubscriptions.Any())
        {
            await context.UserSubscriptions.AddRangeAsync(userSubscriptions);
            await context.SaveChangesAsync();
        }
    }
}
