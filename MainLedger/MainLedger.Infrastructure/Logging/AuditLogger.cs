using log4net;
using System.Text.Json;

namespace MainLedger.Infrastructure.Logging;

/// <summary>
/// Helper class for writing audit logs to the database.
/// </summary>
public static class AuditLogger
{
    private static readonly ILog _auditLogger = LogManager.GetLogger("AuditLogger");

    /// <summary>
    /// Log an audit event for entity creation.
    /// </summary>
    public static void LogCreate(
        Guid userId,
        string entityType,
        string entityId,
        object newValues,
        string? ipAddress = null,
        string? userAgent = null)
    {
        LogAudit(userId, "CREATE", entityType, entityId, null, newValues, ipAddress, userAgent);
    }

    /// <summary>
    /// Log an audit event for entity update.
    /// </summary>
    public static void LogUpdate(
        Guid userId,
        string entityType,
        string entityId,
        object oldValues,
        object newValues,
        string? ipAddress = null,
        string? userAgent = null)
    {
        LogAudit(userId, "UPDATE", entityType, entityId, oldValues, newValues, ipAddress, userAgent);
    }

    /// <summary>
    /// Log an audit event for entity deletion.
    /// </summary>
    public static void LogDelete(
        Guid userId,
        string entityType,
        string entityId,
        object oldValues,
        string? ipAddress = null,
        string? userAgent = null)
    {
        LogAudit(userId, "DELETE", entityType, entityId, oldValues, null, ipAddress, userAgent);
    }

    /// <summary>
    /// Log a custom audit event.
    /// </summary>
    public static void LogCustom(
        Guid userId,
        string action,
        string entityType,
        string entityId,
        object? oldValues = null,
        object? newValues = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        LogAudit(userId, action, entityType, entityId, oldValues, newValues, ipAddress, userAgent);
    }

    private static void LogAudit(
        Guid userId,
        string action,
        string entityType,
        string entityId,
        object? oldValues,
        object? newValues,
        string? ipAddress,
        string? userAgent)
    {
        try
        {
            // Set log context properties
            LogicalThreadContext.Properties["user_id"] = userId.ToString();
            LogicalThreadContext.Properties["action"] = action;
            LogicalThreadContext.Properties["entity_type"] = entityType;
            LogicalThreadContext.Properties["entity_id"] = entityId;
            LogicalThreadContext.Properties["old_values"] = oldValues != null 
                ? JsonSerializer.Serialize(oldValues) 
                : null;
            LogicalThreadContext.Properties["new_values"] = newValues != null 
                ? JsonSerializer.Serialize(newValues) 
                : null;
            LogicalThreadContext.Properties["ip_address"] = ipAddress;
            LogicalThreadContext.Properties["user_agent"] = userAgent;

            // Write audit log
            _auditLogger.Info($"{action} {entityType} {entityId}");
        }
        finally
        {
            // Clear context properties
            LogicalThreadContext.Properties.Remove("user_id");
            LogicalThreadContext.Properties.Remove("action");
            LogicalThreadContext.Properties.Remove("entity_type");
            LogicalThreadContext.Properties.Remove("entity_id");
            LogicalThreadContext.Properties.Remove("old_values");
            LogicalThreadContext.Properties.Remove("new_values");
            LogicalThreadContext.Properties.Remove("ip_address");
            LogicalThreadContext.Properties.Remove("user_agent");
        }
    }
}
