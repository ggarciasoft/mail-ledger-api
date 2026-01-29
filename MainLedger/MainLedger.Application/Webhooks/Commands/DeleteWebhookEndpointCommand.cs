using MainLedger.Domain.Repositories;
using MediatR;

namespace MainLedger.Application.Webhooks.Commands;

public record DeleteWebhookEndpointCommand(
    Guid Id,
    Guid UserId
) : IRequest;

public class DeleteWebhookEndpointCommandHandler : IRequestHandler<DeleteWebhookEndpointCommand>
{
    private readonly IWebhookEndpointRepository _webhookEndpointRepository;

    public DeleteWebhookEndpointCommandHandler(IWebhookEndpointRepository webhookEndpointRepository)
    {
        _webhookEndpointRepository = webhookEndpointRepository;
    }

    public async Task Handle(DeleteWebhookEndpointCommand request, CancellationToken cancellationToken)
    {
        var webhookEndpoint = await _webhookEndpointRepository.GetByIdAsync(request.Id, cancellationToken);

        if (webhookEndpoint == null)
        {
            throw new KeyNotFoundException($"Webhook endpoint with ID {request.Id} not found.");
        }

        // Ensure user owns this webhook
        if (webhookEndpoint.UserId != request.UserId)
        {
            throw new UnauthorizedAccessException("You do not have permission to delete this webhook endpoint.");
        }

        await _webhookEndpointRepository.DeleteAsync(webhookEndpoint, cancellationToken);
    }
}
