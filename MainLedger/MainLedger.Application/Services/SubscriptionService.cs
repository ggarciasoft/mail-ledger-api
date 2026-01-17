using MainLedger.Application.Common.Interfaces;
using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using MainLedger.Domain.ValueObjects;

namespace MainLedger.Application.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly IUserSubscriptionRepository _subscriptionRepository;
    private readonly IApiKeyRepository _apiKeyRepository;
    private readonly IEmailConnectionRepository _emailConnectionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SubscriptionService(
        IUserSubscriptionRepository subscriptionRepository,
        IApiKeyRepository apiKeyRepository,
        IEmailConnectionRepository emailConnectionRepository,
        IUnitOfWork unitOfWork
    )
    {
        _subscriptionRepository = subscriptionRepository;
        _apiKeyRepository = apiKeyRepository;
        _emailConnectionRepository = emailConnectionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SubscriptionLimits> GetUserLimitsAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var subscription = await GetOrCreateSubscriptionAsync(userId, cancellationToken);
        return SubscriptionLimits.FromPlan(subscription.SubscriptionPlan);
    }

    public async Task<bool> CanProcessEmailAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var subscription = await GetOrCreateSubscriptionAsync(userId, cancellationToken);
        var limit = subscription.SubscriptionPlan.MonthlyEmailLimit;

        // Unlimited for enterprise
        if (limit == int.MaxValue)
            return true;

        return !subscription.HasReachedEmailLimit(limit);
    }

    public async Task IncrementEmailCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var subscription = await GetOrCreateSubscriptionAsync(userId, cancellationToken);
        subscription.IncrementEmailCount();
        _subscriptionRepository.Update(subscription);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> CanCreateApiKeyAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var subscription = await GetOrCreateSubscriptionAsync(userId, cancellationToken);
        var maxKeys = subscription.SubscriptionPlan.MaxApiKeys;

        // Not allowed
        if (maxKeys == 0)
            return false;

        // Unlimited
        if (maxKeys == int.MaxValue)
            return true;

        var currentCount = await _apiKeyRepository.CountByUserIdAsync(userId, cancellationToken);
        return currentCount < maxKeys;
    }

    public async Task<bool> CanConnectGmailAccountAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var subscription = await GetOrCreateSubscriptionAsync(userId, cancellationToken);
        var maxAccounts = subscription.SubscriptionPlan.MaxEmailAccounts;

        // Unlimited
        if (maxAccounts == int.MaxValue)
            return true;

        var currentCount = await _emailConnectionRepository.CountByUserAndProviderAsync(
            userId,
            EmailProvider.Gmail,
            cancellationToken
        );
        return currentCount < maxAccounts;
    }

    public async Task<bool> CanExportAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var subscription = await GetOrCreateSubscriptionAsync(userId, cancellationToken);
        return subscription.SubscriptionPlan.CanExport;
    }

    public async Task<bool> CanUseWorkflowAutomationAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var subscription = await GetOrCreateSubscriptionAsync(userId, cancellationToken);
        return subscription.SubscriptionPlan.CanUseWorkflowAutomation;
    }

    public async Task<bool> CanUseWebhooksAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var subscription = await GetOrCreateSubscriptionAsync(userId, cancellationToken);
        return subscription.SubscriptionPlan.CanUseWebhooks;
    }

    public async Task<bool> CanUseBulkOperationsAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var subscription = await GetOrCreateSubscriptionAsync(userId, cancellationToken);
        return subscription.SubscriptionPlan.CanUseBulkOperations;
    }

    public async Task<(int emailsProcessed, int emailLimit)> GetEmailUsageAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var subscription = await GetOrCreateSubscriptionAsync(userId, cancellationToken);
        return (
            subscription.EmailsProcessedThisMonth,
            subscription.SubscriptionPlan.MonthlyEmailLimit
        );
    }

    private async Task<UserSubscription> GetOrCreateSubscriptionAsync(
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        var subscription = await _subscriptionRepository.GetByUserIdAsync(
            userId,
            cancellationToken
        );

        if (subscription == null)
        {
            // Create a free subscription for new users
            subscription = new UserSubscription(userId, SubscriptionPlan.FreePlanId);
            _subscriptionRepository.Add(subscription);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Re-fetch to get the plan details
            subscription = await _subscriptionRepository.GetByUserIdAsync(
                userId,
                cancellationToken
            );
        }

        return subscription!;
    }
}
