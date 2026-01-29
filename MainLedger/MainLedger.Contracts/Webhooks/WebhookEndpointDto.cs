namespace MainLedger.Contracts.Webhooks;

public record WebhookEndpointDto(
    Guid Id,
    string Url,
    List<string> Events,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastTriggeredAt,
    string? SecretKey = null // Only populated on creation
);
