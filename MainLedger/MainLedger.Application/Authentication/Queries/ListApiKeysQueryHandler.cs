using MainLedger.Domain.Repositories;
using MainLedger.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.Authentication.Queries;

public class ListApiKeysQueryHandler : IRequestHandler<ListApiKeysQuery, List<ApiKeyDto>>
{
    private readonly IApiKeyRepository _apiKeyRepository;
    private readonly ILogger<ListApiKeysQueryHandler> _logger;

    public ListApiKeysQueryHandler(
        IApiKeyRepository apiKeyRepository,
        ILogger<ListApiKeysQueryHandler> logger)
    {
        _apiKeyRepository = apiKeyRepository;
        _logger = logger;
    }

    public async Task<List<ApiKeyDto>> Handle(ListApiKeysQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Listing API keys for user {UserId}", request.UserId);

        var apiKeys = await _apiKeyRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        var result = apiKeys.Select(k =>
        {
            // Create a masked version of the key for display
            // Since we only have the hash, we'll create a generic masked display
            var maskedKey = $"mlk_****{k.Id.ToString("N").Substring(0, 4)}";

            return new ApiKeyDto(
                k.Id,
                k.Name,
                maskedKey,
                k.Scopes,
                k.IsActive,
                k.ExpiresAt,
                k.LastUsedAt,
                k.CreatedAt);
        }).ToList();

        return result;
    }
}
