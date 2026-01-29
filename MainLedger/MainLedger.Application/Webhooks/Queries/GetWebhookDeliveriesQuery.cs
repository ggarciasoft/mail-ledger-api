using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using MediatR;

namespace MainLedger.Application.Webhooks.Queries;

public record GetWebhookDeliveriesQuery(
    Guid WebhookEndpointId,
    Guid UserId,
    WebhookDeliveryStatus? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<GetWebhookDeliveriesResult>;

public record GetWebhookDeliveriesResult(
    List<WebhookDeliveryDto> Deliveries,
    int TotalCount,
    int Page,
    int PageSize
);

public record WebhookDeliveryDto(
    Guid Id,
    Guid WebhookEndpointId,
    string EventType,
    string Status,
    int AttemptCount,
    DateTime? LastAttemptAt,
    int? ResponseStatusCode,
    string? ErrorMessage,
    DateTime CreatedAt
);

public class GetWebhookDeliveriesQueryHandler 
    : IRequestHandler<GetWebhookDeliveriesQuery, GetWebhookDeliveriesResult>
{
    private readonly IWebhookEndpointRepository _webhookEndpointRepository;
    private readonly IWebhookDeliveryRepository _webhookDeliveryRepository;

    public GetWebhookDeliveriesQueryHandler(
        IWebhookEndpointRepository webhookEndpointRepository,
        IWebhookDeliveryRepository webhookDeliveryRepository)
    {
        _webhookEndpointRepository = webhookEndpointRepository;
        _webhookDeliveryRepository = webhookDeliveryRepository;
    }

    public async Task<GetWebhookDeliveriesResult> Handle(
        GetWebhookDeliveriesQuery request, 
        CancellationToken cancellationToken)
    {
        // Verify user owns this webhook endpoint
        var endpoint = await _webhookEndpointRepository.GetByIdAsync(
            request.WebhookEndpointId, 
            cancellationToken);

        if (endpoint == null)
        {
            throw new KeyNotFoundException($"Webhook endpoint with ID {request.WebhookEndpointId} not found.");
        }

        if (endpoint.UserId != request.UserId)
        {
            throw new UnauthorizedAccessException("You do not have permission to view deliveries for this webhook endpoint.");
        }

        // Get deliveries
        var (deliveries, totalCount) = await _webhookDeliveryRepository.GetByEndpointIdAsync(
            request.WebhookEndpointId,
            request.Status,
            request.FromDate,
            request.ToDate,
            request.Page,
            request.PageSize,
            cancellationToken);

        var deliveryDtos = deliveries.Select(d => new WebhookDeliveryDto(
            d.Id,
            d.WebhookEndpointId,
            d.EventType.ToString(),
            d.Status.ToString(),
            d.AttemptCount,
            d.LastAttemptAt,
            d.ResponseStatusCode,
            d.ErrorMessage,
            d.CreatedAt
        )).ToList();

        return new GetWebhookDeliveriesResult(
            deliveryDtos,
            totalCount,
            request.Page,
            request.PageSize
        );
    }
}
