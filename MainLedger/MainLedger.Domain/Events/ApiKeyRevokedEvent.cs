using MainLedger.Domain.Common;

namespace MainLedger.Domain.Events;

/// <summary>
/// Event raised when an API key is revoked.
/// </summary>
public sealed class ApiKeyRevokedEvent : DomainEvent
{
    public Guid ApiKeyId { get; }
    public Guid UserId { get; }
    public string Name { get; }

    public ApiKeyRevokedEvent(Guid apiKeyId, Guid userId, string name)
    {
        ApiKeyId = apiKeyId;
        UserId = userId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}
