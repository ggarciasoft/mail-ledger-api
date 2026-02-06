using MainLedger.Domain.Common;

namespace MainLedger.Domain.Entities;

/// <summary>
/// Represents an external OAuth login provider linked to a user account.
/// Used for SSO authentication (Google, Microsoft, etc.)
/// </summary>
public class ExternalLogin : Entity
{
    public Guid UserId { get; private set; }
    public string Provider { get; private set; }
    public string ProviderUserId { get; private set; }
    public string Email { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastUsedAt { get; private set; }

    // Navigation properties
    public User User { get; set; } = null!;

    private ExternalLogin(
        Guid id,
        Guid userId,
        string provider,
        string providerUserId,
        string email,
        DateTime createdAt,
        DateTime? lastUsedAt
    )
        : base(id)
    {
        UserId = userId;
        Provider = provider ?? throw new ArgumentNullException(nameof(provider));
        ProviderUserId = providerUserId ?? throw new ArgumentNullException(nameof(providerUserId));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        CreatedAt = createdAt;
        LastUsedAt = lastUsedAt;
    }

    /// <summary>
    /// Creates a new external login link for a user.
    /// </summary>
    public static ExternalLogin Create(
        Guid userId,
        string provider,
        string providerUserId,
        string email
    )
    {
        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Provider cannot be empty.", nameof(provider));
        if (string.IsNullOrWhiteSpace(providerUserId))
            throw new ArgumentException("Provider user ID cannot be empty.", nameof(providerUserId));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty.", nameof(email));

        return new ExternalLogin(
            Guid.NewGuid(),
            userId,
            provider,
            providerUserId,
            email,
            DateTime.UtcNow,
            null
        );
    }

    /// <summary>
    /// Records that this external login was used for authentication.
    /// </summary>
    public void RecordUsage()
    {
        LastUsedAt = DateTime.UtcNow;
    }

    // For EF Core
    private ExternalLogin()
        : base()
    {
        Provider = null!;
        ProviderUserId = null!;
        Email = null!;
    }
}
