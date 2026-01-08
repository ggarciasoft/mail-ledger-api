using MainLedger.Domain.Common;

namespace MainLedger.Domain.Entities;

/// <summary>
/// Represents an immutable audit log entry for compliance and tracking.
/// Records all significant actions in the system.
/// </summary>
public sealed class AuditLog : Entity
{
    public Guid UserId { get; private set; }
    public string EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public string Action { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string? Changes { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }

    private AuditLog(
        Guid id,
        Guid userId,
        string entityType,
        Guid entityId,
        string action,
        DateTime timestamp,
        string? changes,
        string? ipAddress,
        string? userAgent) : base(id)
    {
        if (string.IsNullOrWhiteSpace(entityType))
            throw new ArgumentException("Entity type cannot be empty.", nameof(entityType));
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action cannot be empty.", nameof(action));

        UserId = userId;
        EntityType = entityType;
        EntityId = entityId;
        Action = action;
        Timestamp = timestamp;
        Changes = changes;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }

    /// <summary>
    /// Creates a new audit log entry.
    /// </summary>
    public static AuditLog Create(
        Guid userId,
        string entityType,
        Guid entityId,
        string action,
        string? changes = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        return new AuditLog(
            Guid.NewGuid(),
            userId,
            entityType,
            entityId,
            action,
            DateTime.UtcNow,
            changes,
            ipAddress,
            userAgent);
    }

    // For EF Core
    private AuditLog() : base() 
    {
        EntityType = null!;
        Action = null!;
    }
}

