using MainLedger.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.Authentication.Commands;

public class RevokeApiKeyCommandHandler : IRequestHandler<RevokeApiKeyCommand, bool>
{
    private readonly IApiKeyRepository _apiKeyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RevokeApiKeyCommandHandler> _logger;

    public RevokeApiKeyCommandHandler(
        IApiKeyRepository apiKeyRepository,
        IUnitOfWork unitOfWork,
        ILogger<RevokeApiKeyCommandHandler> logger)
    {
        _apiKeyRepository = apiKeyRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(RevokeApiKeyCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Revoking API key {ApiKeyId} for user {UserId}", request.ApiKeyId, request.UserId);

        // Get API key
        var apiKey = await _apiKeyRepository.GetByIdAsync(request.ApiKeyId, cancellationToken);
        if (apiKey == null)
        {
            throw new KeyNotFoundException($"API key {request.ApiKeyId} not found.");
        }

        // Verify ownership
        if (apiKey.UserId != request.UserId)
        {
            throw new UnauthorizedAccessException($"User {request.UserId} does not own API key {request.ApiKeyId}.");
        }

        // Revoke
        apiKey.Revoke();

        // Save
        await _apiKeyRepository.UpdateAsync(apiKey, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("API key {ApiKeyId} revoked", request.ApiKeyId);

        // TODO: Publish ApiKeyRevokedEvent

        return true;
    }
}
