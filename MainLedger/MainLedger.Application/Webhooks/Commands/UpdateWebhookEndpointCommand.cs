using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using MediatR;

namespace MainLedger.Application.Webhooks.Commands;

public record UpdateWebhookEndpointCommand(
    Guid Id,
    Guid UserId,
    string Url,
    List<WebhookEventType> Events
) : IRequest;

public class UpdateWebhookEndpointCommandHandler : IRequestHandler<UpdateWebhookEndpointCommand>
{
    private readonly IWebhookEndpointRepository _webhookEndpointRepository;

    public UpdateWebhookEndpointCommandHandler(IWebhookEndpointRepository webhookEndpointRepository)
    {
        _webhookEndpointRepository = webhookEndpointRepository;
    }

    public async Task Handle(UpdateWebhookEndpointCommand request, CancellationToken cancellationToken)
    {
        var webhookEndpoint = await _webhookEndpointRepository.GetByIdAsync(request.Id, cancellationToken);

        if (webhookEndpoint == null)
        {
            throw new KeyNotFoundException($"Webhook endpoint with ID {request.Id} not found.");
        }

        // Ensure user owns this webhook
        if (webhookEndpoint.UserId != request.UserId)
        {
            throw new UnauthorizedAccessException("You do not have permission to update this webhook endpoint.");
        }

        // Validate URL
        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) || 
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("Invalid webhook URL. Must be a valid HTTP or HTTPS URL.");
        }

        // Validate events
        if (request.Events == null || !request.Events.Any())
        {
            throw new ArgumentException("At least one event type must be specified.");
        }

        // Update properties
        webhookEndpoint.Url = request.Url;
        webhookEndpoint.Events = request.Events;

        await _webhookEndpointRepository.UpdateAsync(webhookEndpoint, cancellationToken);
    }
}
