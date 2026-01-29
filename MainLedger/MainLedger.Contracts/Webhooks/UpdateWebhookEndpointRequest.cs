namespace MainLedger.Contracts.Webhooks;

public record UpdateWebhookEndpointRequest(
    string Url,
    List<string> Events
);
