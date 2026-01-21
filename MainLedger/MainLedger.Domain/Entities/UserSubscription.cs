using MainLedger.Domain.Common;
using MainLedger.Domain.Enums;

namespace MainLedger.Domain.Entities;

/// <summary>
/// Represents a user's subscription to a plan with usage tracking.
/// </summary>
public class UserSubscription : Entity
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public Guid SubscriptionPlanId { get; private set; }
    public SubscriptionPlan SubscriptionPlan { get; private set; } = null!;

    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public SubscriptionStatus Status { get; private set; }

    // Usage tracking
    public int EmailsClassifiedThisMonth { get; private set; }
    public int EmailsExtractedThisMonth { get; private set; }
    public DateTime CurrentPeriodStart { get; private set; }
    public DateTime CurrentPeriodEnd { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private UserSubscription() { } // EF Core

    public UserSubscription(Guid userId, Guid subscriptionPlanId)
    {
        UserId = userId;
        SubscriptionPlanId = subscriptionPlanId;
        StartDate = DateTime.UtcNow;
        Status = SubscriptionStatus.Active;
        EmailsClassifiedThisMonth = 0;
        EmailsExtractedThisMonth = 0;
        CurrentPeriodStart = DateTime.UtcNow;
        CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1);
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementClassifiedCount()
    {
        // Check if we need to reset the monthly counter
        if (DateTime.UtcNow > CurrentPeriodEnd)
        {
            ResetMonthlyUsage();
        }

        EmailsClassifiedThisMonth++;
    }

    public void IncrementExtractedCount()
    {
        // Check if we need to reset the monthly counter
        if (DateTime.UtcNow > CurrentPeriodEnd)
        {
            ResetMonthlyUsage();
        }

        EmailsExtractedThisMonth++;
    }

    public void ResetMonthlyUsage()
    {
        EmailsClassifiedThisMonth = 0;
        EmailsExtractedThisMonth = 0;
        CurrentPeriodStart = DateTime.UtcNow;
        CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1);
    }

    public bool HasReachedClassificationLimit(int limit)
    {
        // Check if we need to reset first
        if (DateTime.UtcNow > CurrentPeriodEnd)
        {
            ResetMonthlyUsage();
        }

        return EmailsClassifiedThisMonth >= limit;
    }

    public bool HasReachedExtractionLimit(int limit)
    {
        // Check if we need to reset first
        if (DateTime.UtcNow > CurrentPeriodEnd)
        {
            ResetMonthlyUsage();
        }

        return EmailsExtractedThisMonth >= limit;
    }

    public void Upgrade(Guid newPlanId)
    {
        SubscriptionPlanId = newPlanId;
        // Keep the same billing period
    }

    public void Cancel()
    {
        Status = SubscriptionStatus.Cancelled;
        EndDate = CurrentPeriodEnd; // Cancel at end of current period
    }

    public void Expire()
    {
        Status = SubscriptionStatus.Expired;
        EndDate = DateTime.UtcNow;
    }

    public void Reactivate(Guid planId)
    {
        SubscriptionPlanId = planId;
        Status = SubscriptionStatus.Active;
        EndDate = null;
        StartDate = DateTime.UtcNow;
        ResetMonthlyUsage();
    }
}
