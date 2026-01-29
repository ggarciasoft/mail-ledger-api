using Hangfire;
using MainLedger.Application.Common.Interfaces;
using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.BackgroundJobs;

/// <summary>
/// Background job for syncing emails from email providers (Gmail, Outlook, etc.).
/// </summary>
public class EmailSyncBackgroundJob(
    IEmailProviderFactory _providerFactory,
    IEmailConnectionRepository _connectionRepository,
    IProcessingJobRepository _jobRepository,
    IEmailSyncHistoryRepository _syncHistoryRepository,
    IJobNotificationService _jobNotificationService,
    ISubscriptionService _subscriptionService,
    IEmailService _emailService,
    IUserRepository _userRepository,
    IUnitOfWork _unitOfWork,
    IRuleRepository _ruleRepository,
    ILogger<EmailSyncBackgroundJob> _logger
    )
{
    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync(
        Guid jobId,
        Guid userId,
        EmailProvider provider,
        int maxEmails,
        CancellationToken cancellationToken
    )
    {
        var job = await _jobRepository.GetByIdAsync(jobId, cancellationToken);
        if (job == null)
        {
            _logger.LogError("Job {JobId} not found", jobId);
            return;
        }

        try
        {
            _logger.LogInformation(
                "Starting email sync job {JobId} for user {UserId} with provider {Provider}",
                jobId,
                userId,
                provider
            );

            var connection = await _connectionRepository.GetByUserAndProviderAsync(
                userId,
                provider
            );
            if (connection == null)
            {
                job.Fail($"No active {provider} connection found.");
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            // Get the email provider
            var emailProvider = _providerFactory.GetProvider(provider);

            // Load user's active rules
            var rules = await _ruleRepository.GetActiveByUserIdAsync(userId, cancellationToken);

            // Create sync history record
            var syncHistory = EmailSyncHistory.Create(userId, provider, null);
            await _syncHistoryRepository.AddAsync(syncHistory, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Sync emails using provider-specific internal methods
            var syncOptions = new Domain.Services.SyncOptions
            {
                SyncFrom = connection.LastSyncedAt,
                MaxResults = maxEmails
            };

            // Call provider's internal sync method (not the public API which enqueues jobs)
            var syncResult = await emailProvider.PerformSyncAsync(userId, syncOptions, cancellationToken);

            // Check subscription limits
            var canProcessEmails = await _subscriptionService.CanProcessEmailAsync(
                userId,
                cancellationToken
            );
            if (!canProcessEmails)
            {
                job.Fail(
                    "Monthly email processing limit reached. Please upgrade your subscription."
                );
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _jobNotificationService.NotifyJobFailed(userId, job);
                _logger.LogWarning("User {UserId} has reached their monthly email limit", userId);
                return;
            }

            // Update job with sync results
            var totalEmails = syncResult.EmailsSynced + syncResult.EmailsSkipped;
            job.Start(totalEmails);
            job.UpdateProgress(totalEmails, syncResult.EmailsSynced, syncResult.EmailsSkipped);
            job.Complete();

            // Complete sync history
            syncHistory.Complete(totalEmails, syncResult.EmailsSynced, true);
            await _syncHistoryRepository.UpdateAsync(syncHistory, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Notify clients of job completion
            await _jobNotificationService.NotifyJobCompleted(userId, job);

            // Send email notification if user has enabled it
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user != null && user.EmailNotificationsEnabled && user.NotifyOnEmailSync)
            {
                await _emailService.QueueEmailAsync(
                    user.Email.Value,
                    EmailType.EmailSyncComplete,
                    new Dictionary<string, string>
                    {
                        { "EmailCount", totalEmails.ToString() },
                        { "NewEmailCount", syncResult.EmailsSynced.ToString() },
                        { "Provider", provider.ToString() },
                    },
                    cancellationToken
                );
            }

            _logger.LogInformation(
                "{Provider} sync job {JobId} completed: {Total} total, {Synced} synced, {Skipped} skipped",
                provider,
                jobId,
                totalEmails,
                syncResult.EmailsSynced,
                syncResult.EmailsSkipped
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email sync job {JobId} failed", jobId);
            job.Fail(ex.Message);

            // Try to mark sync history as failed
            try
            {
                var failedSyncHistory = await _syncHistoryRepository.GetLatestByUserIdAsync(
                    userId,
                    cancellationToken
                );
                if (failedSyncHistory != null && failedSyncHistory.SyncCompletedAt == null)
                {
                    failedSyncHistory.Complete(0, 0, false, ex.Message);
                    await _syncHistoryRepository.UpdateAsync(failedSyncHistory, cancellationToken);
                }
            }
            catch
            {
                // Ignore errors updating sync history on failure
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Notify clients of job failure
            await _jobNotificationService.NotifyJobFailed(userId, job);

            throw;
        }
    }
}
