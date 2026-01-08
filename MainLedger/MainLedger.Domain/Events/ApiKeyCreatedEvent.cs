using MainLedger.Domain.Common;

namespace MainLedger.Domain.Events;

/// <summary>
/// Event raised when an API key is created.
/// </summary>
public sealed class ApiKeyCreatedEvent : DomainEvent
{
    public Guid ApiKeyId { get; }
    public Guid UserId { get; }
    public string Name { get; }
    public string[] Scopes { get; }
    public DateTime? ExpiresAt { get; }

    public ApiKeyCreatedEvent(Guid apiKeyId, Guid userId, string name, string[] scopes, DateTime? expiresAt)
    {
        ApiKeyId = apiKeyId;
        UserId = userId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        ExpiresAt = expiresAt;
    }
}
