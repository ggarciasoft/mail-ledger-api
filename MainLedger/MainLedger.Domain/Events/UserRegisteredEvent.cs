using MainLedger.Domain.Common;

namespace MainLedger.Domain.Events;

/// <summary>
/// Event raised when a new user registers.
/// </summary>
public sealed class UserRegisteredEvent : DomainEvent
{
    public Guid UserId { get; }
    public string Email { get; }
    public string FirstName { get; }
    public string LastName { get; }

    public UserRegisteredEvent(Guid userId, string email, string firstName, string lastName)
    {
        UserId = userId;
        Email = email ?? throw new ArgumentNullException(nameof(email));
        FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
        LastName = lastName ?? throw new ArgumentNullException(nameof(lastName));
    }
}
