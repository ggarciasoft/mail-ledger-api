using MainLedger.Domain.Common;

namespace MainLedger.Domain.Entities;

/// <summary>
/// Represents a refresh token for JWT authentication.
/// Used to obtain new access tokens without re-authentication.
/// </summary>
public sealed class RefreshToken : Entity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    private RefreshToken(
        Guid id,
        Guid userId,
        string tokenHash,
        DateTime expiresAt,
        bool isRevoked,
        DateTime createdAt,
        DateTime? revokedAt) : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash ?? throw new ArgumentNullException(nameof(tokenHash));
        ExpiresAt = expiresAt;
        IsRevoked = isRevoked;
        CreatedAt = createdAt;
        RevokedAt = revokedAt;
    }

    /// <summary>
    /// Creates a new refresh token.
    /// </summary>
    public static RefreshToken Create(Guid userId, string tokenHash, DateTime expiresAt)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException("Token hash cannot be empty.", nameof(tokenHash));
        if (expiresAt <= DateTime.UtcNow)
            throw new ArgumentException("Expiration date must be in the future.", nameof(expiresAt));

        return new RefreshToken(
            Guid.NewGuid(),
            userId,
            tokenHash,
            expiresAt,
            isRevoked: false,
            createdAt: DateTime.UtcNow,
            revokedAt: null);
    }

    /// <summary>
    /// Revokes the refresh token.
    /// </summary>
    public void Revoke()
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the refresh token is valid (not revoked and not expired).
    /// </summary>
    public bool IsValid()
    {
        if (IsRevoked)
            return false;

        if (ExpiresAt <= DateTime.UtcNow)
            return false;

        return true;
    }

    // For EF Core
    private RefreshToken() : base() 
    {
        TokenHash = null!;
    }
}
