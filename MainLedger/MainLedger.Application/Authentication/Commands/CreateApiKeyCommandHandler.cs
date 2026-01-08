using MainLedger.Domain.Entities;
using MainLedger.Domain.Repositories;
using MainLedger.Domain.Services;
using MainLedger.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.Authentication.Commands;

public class CreateApiKeyCommandHandler : IRequestHandler<CreateApiKeyCommand, CreateApiKeyResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IApiKeyRepository _apiKeyRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateApiKeyCommandHandler> _logger;

    public CreateApiKeyCommandHandler(
        IUserRepository userRepository,
        IApiKeyRepository apiKeyRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        ILogger<CreateApiKeyCommandHandler> logger)
    {
        _userRepository = userRepository;
        _apiKeyRepository = apiKeyRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CreateApiKeyResult> Handle(CreateApiKeyCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating API key for user {UserId}", request.UserId);

        // Verify user exists
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException($"User {request.UserId} not found.");
        }

        if (!user.IsActive)
        {
            throw new InvalidOperationException("Cannot create API key for inactive user.");
        }

        // Validate scopes
        if (request.Scopes == null || request.Scopes.Length == 0)
        {
            throw new ArgumentException("At least one scope is required.");
        }

        // Generate API key
        var apiKeyValue = ApiKeyValue.Generate("live");
        var keyHash = _passwordHasher.HashPassword(apiKeyValue.Value);

        // Create API key entity
        var apiKey = ApiKey.Create(
            request.UserId,
            keyHash,
            request.Name,
            request.Scopes,
            request.ExpiresAt);

        // Save
        await _apiKeyRepository.AddAsync(apiKey, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("API key {ApiKeyId} created for user {UserId}", apiKey.Id, request.UserId);

        // TODO: Publish ApiKeyCreatedEvent

        // Return the plain API key (only time it's visible!)
        return new CreateApiKeyResult(
            apiKey.Id,
            apiKeyValue.Value,
            apiKey.Name,
            apiKey.Scopes,
            apiKey.ExpiresAt);
    }
}
