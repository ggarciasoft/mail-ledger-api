using MainLedger.Domain.Common;

namespace MainLedger.Domain.Events;

/// <summary>
/// Event raised when a user's email is verified.
/// </summary>
public sealed class EmailVerifiedEvent : DomainEvent
{
    public Guid UserId { get; }
    public string Email { get; }

    public EmailVerifiedEvent(Guid userId, string email)
    {
        UserId = userId;
        Email = email ?? throw new ArgumentNullException(nameof(email));
    }
}
