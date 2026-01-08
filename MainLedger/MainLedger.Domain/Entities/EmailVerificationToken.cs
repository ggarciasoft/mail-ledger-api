using MainLedger.Domain.Common;

namespace MainLedger.Domain.Entities;

/// <summary>
/// Represents a token for email verification.
/// Single-use token with expiration.
/// </summary>
public sealed class EmailVerificationToken : Entity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? UsedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private EmailVerificationToken(
        Guid id,
        Guid userId,
        string tokenHash,
        DateTime expiresAt,
        DateTime? usedAt,
        DateTime createdAt) : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash ?? throw new ArgumentNullException(nameof(tokenHash));
        ExpiresAt = expiresAt;
        UsedAt = usedAt;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Creates a new email verification token.
    /// Default expiration is 24 hours.
    /// </summary>
    public static EmailVerificationToken Create(Guid userId, string tokenHash, TimeSpan? expiresIn = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException("Token hash cannot be empty.", nameof(tokenHash));

        var expiration = expiresIn ?? TimeSpan.FromHours(24);
        var expiresAt = DateTime.UtcNow.Add(expiration);

        return new EmailVerificationToken(
            Guid.NewGuid(),
            userId,
            tokenHash,
            expiresAt,
            usedAt: null,
            createdAt: DateTime.UtcNow);
    }

    /// <summary>
    /// Marks the token as used.
    /// </summary>
    public void MarkAsUsed()
    {
        if (UsedAt.HasValue)
            throw new InvalidOperationException("Token has already been used.");

        if (!IsValid())
            throw new InvalidOperationException("Token is expired and cannot be used.");

        UsedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the token is valid (not used and not expired).
    /// </summary>
    public bool IsValid()
    {
        if (UsedAt.HasValue)
            return false;

        if (ExpiresAt <= DateTime.UtcNow)
            return false;

        return true;
    }

    // For EF Core
    private EmailVerificationToken() : base() 
    {
        TokenHash = null!;
    }
}
