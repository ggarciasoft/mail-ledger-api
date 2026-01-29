using MainLedger.Application.Common.Interfaces;
using MainLedger.Contracts.Subscriptions;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using MediatR;

namespace MainLedger.Application.Subscriptions.Queries.GetSubscriptionUsage;

public class GetSubscriptionUsageQueryHandler
    : IRequestHandler<GetSubscriptionUsageQuery, SubscriptionUsageDto>
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IApiKeyRepository _apiKeyRepository;
    private readonly IEmailConnectionRepository _emailConnectionRepository;
    private readonly IUserSubscriptionRepository _userSubscriptionRepository;
    private readonly IWebhookEndpointRepository _webhookEndpointRepository;

    public GetSubscriptionUsageQueryHandler(
        ISubscriptionService subscriptionService,
        IApiKeyRepository apiKeyRepository,
        IEmailConnectionRepository emailConnectionRepository,
        IUserSubscriptionRepository userSubscriptionRepository,
        IWebhookEndpointRepository webhookEndpointRepository
    )
    {
        _subscriptionService = subscriptionService;
        _apiKeyRepository = apiKeyRepository;
        _emailConnectionRepository = emailConnectionRepository;
        _userSubscriptionRepository = userSubscriptionRepository;
        _webhookEndpointRepository = webhookEndpointRepository;
    }

    public async Task<SubscriptionUsageDto> Handle(
        GetSubscriptionUsageQuery request,
        CancellationToken cancellationToken
    )
    {
        var limits = await _subscriptionService.GetUserLimitsAsync(
            request.UserId,
            cancellationToken
        );

        // Get the user's subscription to access usage and limits
        var subscription = await _userSubscriptionRepository.GetByUserIdAsync(
            request.UserId,
            cancellationToken
        );

        if (subscription == null)
        {
            throw new InvalidOperationException("User subscription not found");
        }

        var apiKeysCount = await _apiKeyRepository.CountByUserIdAsync(
            request.UserId,
            cancellationToken
        );
        var emailAccountsCount = await _emailConnectionRepository.CountByUserAndProviderAsync(
            request.UserId,
            EmailProvider.Gmail,
            cancellationToken
        );
        var webhooksCount = await _webhookEndpointRepository.CountByUserIdAsync(
            request.UserId,
            cancellationToken
        );

        return new SubscriptionUsageDto(
            subscription.EmailsClassifiedThisMonth,
            subscription.EmailsExtractedThisMonth,
            subscription.SubscriptionPlan.ClassificationLimit,
            subscription.SubscriptionPlan.ExtractionLimit,
            emailAccountsCount,
            limits.MaxEmailAccounts,
            apiKeysCount,
            limits.MaxApiKeys,
            webhooksCount,
            limits.MaxWebhooks
        );
    }
}
