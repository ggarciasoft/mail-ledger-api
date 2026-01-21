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

    public GetSubscriptionUsageQueryHandler(
        ISubscriptionService subscriptionService,
        IApiKeyRepository apiKeyRepository,
        IEmailConnectionRepository emailConnectionRepository
    )
    {
        _subscriptionService = subscriptionService;
        _apiKeyRepository = apiKeyRepository;
        _emailConnectionRepository = emailConnectionRepository;
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
        var (emailsClassified, classificationLimit) = await _subscriptionService.GetEmailUsageAsync(
            request.UserId,
            cancellationToken
        );

        var apiKeysCount = await _apiKeyRepository.CountByUserIdAsync(
            request.UserId,
            cancellationToken
        );
        var emailAccountsCount = await _emailConnectionRepository.CountByUserAndProviderAsync(
            request.UserId,
            EmailProvider.Gmail,
            cancellationToken
        );

        return new SubscriptionUsageDto(
            emailsClassified,
            0, // EmailsExtracted - will be tracked separately in Phase 2
            classificationLimit,
            limits.MaxEmailAccounts, // ExtractionLimit - using MaxEmailAccounts as placeholder
            emailAccountsCount,
            limits.MaxEmailAccounts,
            apiKeysCount,
            limits.MaxApiKeys
        );
    }
}
