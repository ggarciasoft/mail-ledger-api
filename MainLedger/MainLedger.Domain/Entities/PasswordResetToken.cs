using MainLedger.Domain.Common;

namespace MainLedger.Domain.Entities;

/// <summary>
/// Represents a token for password reset.
/// Single-use token with short expiration for security.
/// </summary>
public sealed class PasswordResetToken : Entity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? UsedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private PasswordResetToken(
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
    /// Creates a new password reset token.
    /// Default expiration is 1 hour for security.
    /// </summary>
    public static PasswordResetToken Create(Guid userId, string tokenHash, TimeSpan? expiresIn = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException("Token hash cannot be empty.", nameof(tokenHash));

        var expiration = expiresIn ?? TimeSpan.FromHours(1);
        var expiresAt = DateTime.UtcNow.Add(expiration);

        return new PasswordResetToken(
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
    private PasswordResetToken() : base() 
    {
        TokenHash = null!;
    }
}
