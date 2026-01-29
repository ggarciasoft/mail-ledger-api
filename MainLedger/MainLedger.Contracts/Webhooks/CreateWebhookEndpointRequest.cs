namespace MainLedger.Contracts.Webhooks;

public record CreateWebhookEndpointRequest(
    string Url,
    List<string> Events
);
