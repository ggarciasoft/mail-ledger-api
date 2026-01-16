using MainLedger.Application.Common.Interfaces;
using MainLedger.Contracts.Subscriptions;
using MainLedger.Domain.Repositories;
using MediatR;

namespace MainLedger.Application.Subscriptions.Queries.GetSubscriptionUsage;

public class GetSubscriptionUsageQueryHandler
    : IRequestHandler<GetSubscriptionUsageQuery, SubscriptionUsageDto>
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IApiKeyRepository _apiKeyRepository;
    private readonly IGmailConnectionRepository _gmailConnectionRepository;

    public GetSubscriptionUsageQueryHandler(
        ISubscriptionService subscriptionService,
        IApiKeyRepository apiKeyRepository,
        IGmailConnectionRepository gmailConnectionRepository
    )
    {
        _subscriptionService = subscriptionService;
        _apiKeyRepository = apiKeyRepository;
        _gmailConnectionRepository = gmailConnectionRepository;
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
        var (emailsProcessed, emailLimit) = await _subscriptionService.GetEmailUsageAsync(
            request.UserId,
            cancellationToken
        );

        var apiKeysCount = await _apiKeyRepository.CountByUserIdAsync(
            request.UserId,
            cancellationToken
        );
        var gmailAccountsCount = await _gmailConnectionRepository.CountByUserIdAsync(
            request.UserId,
            cancellationToken
        );

        return new SubscriptionUsageDto(
            emailsProcessed,
            emailLimit,
            gmailAccountsCount,
            limits.MaxGmailAccounts,
            apiKeysCount,
            limits.MaxApiKeys
        );
    }
}
