using MainLedger.Domain.Repositories;
using MediatR;

namespace MainLedger.Application.Webhooks.Queries;

public record GetWebhookEndpointsQuery(Guid UserId) : IRequest<List<WebhookEndpointDto>>;

public record WebhookEndpointDto(
    Guid Id,
    string Url,
    List<string> Events,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastTriggeredAt
);

public class GetWebhookEndpointsQueryHandler 
    : IRequestHandler<GetWebhookEndpointsQuery, List<WebhookEndpointDto>>
{
    private readonly IWebhookEndpointRepository _webhookEndpointRepository;

    public GetWebhookEndpointsQueryHandler(IWebhookEndpointRepository webhookEndpointRepository)
    {
        _webhookEndpointRepository = webhookEndpointRepository;
    }

    public async Task<List<WebhookEndpointDto>> Handle(
        GetWebhookEndpointsQuery request, 
        CancellationToken cancellationToken)
    {
        var endpoints = await _webhookEndpointRepository.GetByUserIdAsync(
            request.UserId, 
            cancellationToken);

        return endpoints.Select(e => new WebhookEndpointDto(
            e.Id,
            e.Url,
            e.Events.Select(ev => ev.ToString()).ToList(),
            e.IsActive,
            e.CreatedAt,
            e.LastTriggeredAt
        )).ToList();
    }
}
