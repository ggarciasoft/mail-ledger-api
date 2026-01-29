namespace MainLedger.Contracts.Webhooks;

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
