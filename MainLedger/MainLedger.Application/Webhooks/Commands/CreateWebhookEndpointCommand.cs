using System.Security.Cryptography;
using MainLedger.Application.Common.Interfaces;
using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using MediatR;

namespace MainLedger.Application.Webhooks.Commands;

public record CreateWebhookEndpointCommand(
    Guid UserId,
    string Url,
    List<WebhookEventType> Events
) : IRequest<CreateWebhookEndpointResult>;

public record CreateWebhookEndpointResult(
    Guid Id,
    string SecretKey // Only returned on creation
);

public class CreateWebhookEndpointCommandHandler 
    : IRequestHandler<CreateWebhookEndpointCommand, CreateWebhookEndpointResult>
{
    private readonly IWebhookEndpointRepository _webhookEndpointRepository;
    private readonly ITokenEncryptionService _encryptionService;
    private const int MaxWebhooksPerUser = 10;

    public CreateWebhookEndpointCommandHandler(
        IWebhookEndpointRepository webhookEndpointRepository,
        ITokenEncryptionService encryptionService)
    {
        _webhookEndpointRepository = webhookEndpointRepository;
        _encryptionService = encryptionService;
    }

    public async Task<CreateWebhookEndpointResult> Handle(
        CreateWebhookEndpointCommand request, 
        CancellationToken cancellationToken)
    {
        // Validate URL
        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) || 
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("Invalid webhook URL. Must be a valid HTTP or HTTPS URL.");
        }

        // Check user's webhook limit
        var existingCount = await _webhookEndpointRepository.CountByUserIdAsync(
            request.UserId, 
            cancellationToken);

        if (existingCount >= MaxWebhooksPerUser)
        {
            throw new InvalidOperationException(
                $"Maximum number of webhooks ({MaxWebhooksPerUser}) reached for this user.");
        }

        // Validate events
        if (request.Events == null || !request.Events.Any())
        {
            throw new ArgumentException("At least one event type must be specified.");
        }

        // Generate secure secret key (32 bytes = 256 bits)
        var secretKeyBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(secretKeyBytes);
        }
        var secretKey = Convert.ToBase64String(secretKeyBytes);

        // Encrypt secret key for storage
        var encryptedSecretKey = _encryptionService.Encrypt(secretKey);

        // Create webhook endpoint
        var webhookEndpoint = new WebhookEndpoint
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Url = request.Url,
            SecretKey = encryptedSecretKey,
            Events = request.Events,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _webhookEndpointRepository.AddAsync(webhookEndpoint, cancellationToken);

        // Return unencrypted secret key (only time it's shown)
        return new CreateWebhookEndpointResult(webhookEndpoint.Id, secretKey);
    }
}
