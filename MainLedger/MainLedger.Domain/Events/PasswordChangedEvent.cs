using MainLedger.Domain.Common;

namespace MainLedger.Domain.Events;

/// <summary>
/// Event raised when a user changes their password.
/// </summary>
public sealed class PasswordChangedEvent : DomainEvent
{
    public Guid UserId { get; }
    public string Email { get; }

    public PasswordChangedEvent(Guid userId, string email)
    {
        UserId = userId;
        Email = email ?? throw new ArgumentNullException(nameof(email));
    }
}
