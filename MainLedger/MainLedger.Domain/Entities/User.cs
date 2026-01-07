using MainLedger.Domain.Common;
using MainLedger.Domain.ValueObjects;

namespace MainLedger.Domain.Entities;

/// <summary>
/// Represents a user of the MailLedger system.
/// </summary>
public sealed class User : Entity
{
    public EmailAddress Email { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private User(Guid id, EmailAddress email, DateTime createdAt, bool isActive) : base(id)
    {
        Email = email ?? throw new ArgumentNullException(nameof(email));
        CreatedAt = createdAt;
        IsActive = isActive;
    }

    /// <summary>
    /// Creates a new user.
    /// </summary>
    public static User Create(EmailAddress email)
    {
        return new User(
            Guid.NewGuid(),
            email,
            DateTime.UtcNow,
            isActive: true);
    }

    /// <summary>
    /// Deactivates the user account.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Activates the user account.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    // For EF Core
    private User() : base() { }
}
