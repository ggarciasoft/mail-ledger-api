using MainLedger.Domain.Repositories;
using MediatR;

namespace MainLedger.Application.Webhooks.Commands;

public record ToggleWebhookEndpointCommand(
    Guid Id,
    Guid UserId,
    bool IsActive
) : IRequest;

public class ToggleWebhookEndpointCommandHandler : IRequestHandler<ToggleWebhookEndpointCommand>
{
    private readonly IWebhookEndpointRepository _webhookEndpointRepository;

    public ToggleWebhookEndpointCommandHandler(IWebhookEndpointRepository webhookEndpointRepository)
    {
        _webhookEndpointRepository = webhookEndpointRepository;
    }

    public async Task Handle(ToggleWebhookEndpointCommand request, CancellationToken cancellationToken)
    {
        var webhookEndpoint = await _webhookEndpointRepository.GetByIdAsync(request.Id, cancellationToken);

        if (webhookEndpoint == null)
        {
            throw new KeyNotFoundException($"Webhook endpoint with ID {request.Id} not found.");
        }

        // Ensure user owns this webhook
        if (webhookEndpoint.UserId != request.UserId)
        {
            throw new UnauthorizedAccessException("You do not have permission to modify this webhook endpoint.");
        }

        webhookEndpoint.IsActive = request.IsActive;

        await _webhookEndpointRepository.UpdateAsync(webhookEndpoint, cancellationToken);
    }
}
