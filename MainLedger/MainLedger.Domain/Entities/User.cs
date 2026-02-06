using MainLedger.Domain.Common;
using MainLedger.Domain.ValueObjects;

namespace MainLedger.Domain.Entities;

/// <summary>
/// Represents a user of the MailLedger system.
/// Aggregate root for authentication and user management.
/// </summary>
public sealed class User : Entity
{
    public EmailAddress Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public bool IsEmailVerified { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    // Email Notification Preferences
    public bool EmailNotificationsEnabled { get; private set; } = true;
    public bool NotifyOnEmailSync { get; private set; } = true;
    public bool NotifyOnClassification { get; private set; } = true;
    public bool NotifyOnExtraction { get; private set; } = true;

    private User(
        Guid id,
        EmailAddress email,
        string passwordHash,
        string firstName,
        string lastName,
        bool isEmailVerified,
        bool isActive,
        DateTime createdAt,
        DateTime updatedAt,
        DateTime? lastLoginAt
    )
        : base(id)
    {
        Email = email ?? throw new ArgumentNullException(nameof(email));
        PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
        FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
        LastName = lastName ?? throw new ArgumentNullException(nameof(lastName));
        IsEmailVerified = isEmailVerified;
        IsActive = isActive;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        LastLoginAt = lastLoginAt;
    }

    /// <summary>
    /// Registers a new user with hashed password.
    /// </summary>
    public static User Register(
        EmailAddress email,
        string passwordHash,
        string firstName,
        string lastName
    )
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty.", nameof(passwordHash));
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be empty.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name cannot be empty.", nameof(lastName));

        var now = DateTime.UtcNow;
        return new User(
            Guid.NewGuid(),
            email,
            passwordHash,
            firstName,
            lastName,
            isEmailVerified: false,
            isActive: false, // User starts inactive until email is verified
            createdAt: now,
            updatedAt: now,
            lastLoginAt: null
        );
    }

    /// <summary>
    /// Registers a new user via SSO (no password required).
    /// </summary>
    public static User RegisterWithSSO(
        EmailAddress email,
        string firstName,
        string lastName
    )
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be empty.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name cannot be empty.", nameof(lastName));

        var now = DateTime.UtcNow;
        return new User(
            Guid.NewGuid(),
            email,
            string.Empty, // SSO users don't have passwords
            firstName,
            lastName,
            isEmailVerified: true, // SSO users are auto-verified
            isActive: true, // SSO users are auto-activated
            createdAt: now,
            updatedAt: now,
            lastLoginAt: null
        );
    }

    /// <summary>
    /// Creates a new user (legacy method for backward compatibility).
    /// </summary>
    [Obsolete("Use Register method instead for new user creation with authentication.")]
    public static User Create(EmailAddress email)
    {
        return new User(
            Guid.NewGuid(),
            email,
            string.Empty, // Will need to be set separately
            string.Empty,
            string.Empty,
            isEmailVerified: false,
            isActive: true,
            createdAt: DateTime.UtcNow,
            updatedAt: DateTime.UtcNow,
            lastLoginAt: null
        );
    }

    /// <summary>
    /// Verifies the user's email address.
    /// </summary>
    public void VerifyEmail()
    {
        IsEmailVerified = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Changes the user's password.
    /// </summary>
    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("Password hash cannot be empty.", nameof(newPasswordHash));

        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Resets the user's password (used for password reset flow).
    /// </summary>
    public void ResetPassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("Password hash cannot be empty.", nameof(newPasswordHash));

        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the user's profile information.
    /// </summary>
    public void UpdateProfile(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be empty.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name cannot be empty.", nameof(lastName));

        FirstName = firstName;
        LastName = lastName;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records a successful login.
    /// </summary>
    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the user account.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the user account.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the user's email notification preferences.
    /// </summary>
    public void UpdateNotificationPreferences(
        bool emailNotificationsEnabled,
        bool notifyOnEmailSync,
        bool notifyOnClassification,
        bool notifyOnExtraction
    )
    {
        EmailNotificationsEnabled = emailNotificationsEnabled;
        NotifyOnEmailSync = notifyOnEmailSync;
        NotifyOnClassification = notifyOnClassification;
        NotifyOnExtraction = notifyOnExtraction;
        UpdatedAt = DateTime.UtcNow;
    }

    // For EF Core
    private User()
        : base()
    {
        Email = null!;
        PasswordHash = null!;
        FirstName = null!;
        LastName = null!;
    }
}
