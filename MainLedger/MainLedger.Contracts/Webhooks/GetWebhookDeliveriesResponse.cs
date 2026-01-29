namespace MainLedger.Contracts.Webhooks;

public record GetWebhookDeliveriesResponse(
    List<WebhookDeliveryDto> Deliveries,
    int TotalCount,
    int Page,
    int PageSize
);
