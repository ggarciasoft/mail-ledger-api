using MainLedger.Domain.Common;

namespace MainLedger.Domain.Entities;

/// <summary>
/// Represents an API key for programmatic access to the Mail Ledger API.
/// Belongs to a User and has scope-based permissions.
/// </summary>
public sealed class ApiKey : Entity
{
    public Guid UserId { get; private set; }
    public string KeyHash { get; private set; }
    public string Name { get; private set; }
    public string[] Scopes { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public DateTime? LastUsedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private ApiKey(
        Guid id,
        Guid userId,
        string keyHash,
        string name,
        string[] scopes,
        bool isActive,
        DateTime? expiresAt,
        DateTime? lastUsedAt,
        DateTime createdAt) : base(id)
    {
        UserId = userId;
        KeyHash = keyHash ?? throw new ArgumentNullException(nameof(keyHash));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        IsActive = isActive;
        ExpiresAt = expiresAt;
        LastUsedAt = lastUsedAt;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Creates a new API key.
    /// </summary>
    public static ApiKey Create(Guid userId, string keyHash, string name, string[] scopes, DateTime? expiresAt = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        if (string.IsNullOrWhiteSpace(keyHash))
            throw new ArgumentException("Key hash cannot be empty.", nameof(keyHash));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        if (scopes == null || scopes.Length == 0)
            throw new ArgumentException("At least one scope is required.", nameof(scopes));

        return new ApiKey(
            Guid.NewGuid(),
            userId,
            keyHash,
            name,
            scopes,
            isActive: true,
            expiresAt,
            lastUsedAt: null,
            createdAt: DateTime.UtcNow);
    }

    /// <summary>
    /// Revokes the API key.
    /// </summary>
    public void Revoke()
    {
        IsActive = false;
    }

    /// <summary>
    /// Records usage of the API key.
    /// </summary>
    public void RecordUsage()
    {
        LastUsedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the API key has a specific scope.
    /// </summary>
    public bool HasScope(string scope)
    {
        if (string.IsNullOrWhiteSpace(scope))
            return false;

        return Scopes.Contains(scope, StringComparer.OrdinalIgnoreCase) ||
               Scopes.Contains("admin:all", StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the API key is valid (active and not expired).
    /// </summary>
    public bool IsValid()
    {
        if (!IsActive)
            return false;

        if (ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow)
            return false;

        return true;
    }

    // For EF Core
    private ApiKey() : base() 
    {
        KeyHash = null!;
        Name = null!;
        Scopes = null!;
    }
}
