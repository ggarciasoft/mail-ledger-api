using MainLedger.Domain.Common;

namespace MainLedger.Domain.Events;

/// <summary>
/// Event raised when a user successfully logs in.
/// </summary>
public sealed class UserLoggedInEvent : DomainEvent
{
    public Guid UserId { get; }
    public string Email { get; }
    public string? IpAddress { get; }
    public string? UserAgent { get; }

    public UserLoggedInEvent(Guid userId, string email, string? ipAddress = null, string? userAgent = null)
    {
        UserId = userId;
        Email = email ?? throw new ArgumentNullException(nameof(email));
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }
}
