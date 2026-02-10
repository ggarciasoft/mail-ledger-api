using log4net;
using MainLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MainLedger.Infrastructure.BackgroundJobs;

/// <summary>
/// Background job to clean up old logs from database tables.
/// </summary>
public class LogCleanupBackgroundJob
{
    private readonly MailLedgerDbContext _context;
    private static readonly ILog _logger = LogManager.GetLogger(typeof(LogCleanupBackgroundJob));

    // Retention policies (configurable via appsettings in future)
    private const int ApplicationLogsRetentionDays = 90;
    private const int ErrorLogsRetentionDays = 180;
    private const int AuditLogsRetentionDays = 2555; // ~7 years for compliance

    public LogCleanupBackgroundJob(MailLedgerDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Execute the log cleanup job.
    /// </summary>
    public async Task ExecuteAsync()
    {
        _logger.Info("Starting log cleanup job");

        try
        {
            var now = DateTime.UtcNow;

            // Clean up application logs
            var applicationLogsCutoff = now.AddDays(-ApplicationLogsRetentionDays);
            var applicationLogsDeleted = await CleanupApplicationLogsAsync(applicationLogsCutoff);
            _logger.Info($"Deleted {applicationLogsDeleted} application logs older than {ApplicationLogsRetentionDays} days");

            // Clean up error logs
            var errorLogsCutoff = now.AddDays(-ErrorLogsRetentionDays);
            var errorLogsDeleted = await CleanupErrorLogsAsync(errorLogsCutoff);
            _logger.Info($"Deleted {errorLogsDeleted} error logs older than {ErrorLogsRetentionDays} days");

            // Clean up audit logs
            var auditLogsCutoff = now.AddDays(-AuditLogsRetentionDays);
            var auditLogsDeleted = await CleanupAuditLogsAsync(auditLogsCutoff);
            _logger.Info($"Deleted {auditLogsDeleted} audit logs older than {AuditLogsRetentionDays} days");

            _logger.Info($"Log cleanup job completed. Total deleted: {applicationLogsDeleted + errorLogsDeleted + auditLogsDeleted}");
        }
        catch (Exception ex)
        {
            _logger.Error("Error during log cleanup job", ex);
            throw;
        }
    }

    private async Task<int> CleanupApplicationLogsAsync(DateTime cutoffDate)
    {
        try
        {
            var deletedCount = await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM application_logs WHERE timestamp < {0}",
                cutoffDate
            );

            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.Error("Error cleaning up application logs", ex);
            return 0;
        }
    }

    private async Task<int> CleanupErrorLogsAsync(DateTime cutoffDate)
    {
        try
        {
            var deletedCount = await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM error_logs WHERE timestamp < {0}",
                cutoffDate
            );

            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.Error("Error cleaning up error logs", ex);
            return 0;
        }
    }

    private async Task<int> CleanupAuditLogsAsync(DateTime cutoffDate)
    {
        try
        {
            var deletedCount = await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM audit_logs_table WHERE timestamp < {0}",
                cutoffDate
            );

            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.Error("Error cleaning up audit logs", ex);
            return 0;
        }
    }
}
