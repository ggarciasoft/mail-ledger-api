using Hangfire;
using MainLedger.Application.Common.Interfaces;
using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.BackgroundJobs;

/// <summary>
/// Background job for syncing emails from Gmail.
/// </summary>
public class EmailSyncBackgroundJob
{
    private readonly IGmailService _gmailService;
    private readonly IGmailConnectionRepository _connectionRepository;
    private readonly IEmailMessageRepository _emailRepository;
    private readonly IRuleRepository _ruleRepository;
    private readonly IRulesEngine _rulesEngine;
    private readonly IProcessingJobRepository _jobRepository;
    private readonly IGmailSyncHistoryRepository _syncHistoryRepository;
    private readonly IJobNotificationService _jobNotificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EmailSyncBackgroundJob> _logger;

    public EmailSyncBackgroundJob(
        IGmailService gmailService,
        IGmailConnectionRepository connectionRepository,
        IEmailMessageRepository emailRepository,
        IRuleRepository ruleRepository,
        IRulesEngine rulesEngine,
        IProcessingJobRepository jobRepository,
        IGmailSyncHistoryRepository syncHistoryRepository,
        IJobNotificationService jobNotificationService,
        IUnitOfWork unitOfWork,
        ILogger<EmailSyncBackgroundJob> logger
    )
    {
        _gmailService = gmailService;
        _connectionRepository = connectionRepository;
        _emailRepository = emailRepository;
        _ruleRepository = ruleRepository;
        _rulesEngine = rulesEngine;
        _jobRepository = jobRepository;
        _syncHistoryRepository = syncHistoryRepository;
        _jobNotificationService = jobNotificationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync(
        Guid jobId,
        Guid userId,
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
                "Starting email sync job {JobId} for user {UserId}",
                jobId,
                userId
            );

            var connection = await _connectionRepository.GetActiveByUserIdAsync(
                userId,
                cancellationToken
            );
            if (connection == null)
            {
                job.Fail("No active Gmail connection found.");
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            // Load user's active rules
            var rules = await _ruleRepository.GetActiveByUserIdAsync(userId, cancellationToken);

            // Create sync history record
            var syncHistory = GmailSyncHistory.Create(userId, connection.HistoryId);
            await _syncHistoryRepository.AddAsync(syncHistory, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Fetch emails
            var fetchedEmails = await _gmailService.FetchEmailsAsync(
                connection,
                rules.Any() ? rules : null,
                connection.LastSyncedAt,
                null,
                maxEmails,
                cancellationToken
            );

            job.Start(fetchedEmails.Count);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            int savedCount = 0;
            int ignoredCount = 0;
            int processedCount = 0;

            foreach (var email in fetchedEmails)
            {
                // Check if job has been cancelled
                var currentJob = await _jobRepository.GetByIdAsync(jobId, cancellationToken);
                if (currentJob?.Status == Domain.Enums.JobStatus.Cancelled)
                {
                    _logger.LogInformation("Job {JobId} was cancelled, stopping email sync", jobId);
                    return;
                }

                // Check cancellation token
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation(
                        "Cancellation requested for job {JobId}, stopping email sync",
                        jobId
                    );
                    job.Cancel();
                    await _unitOfWork.SaveChangesAsync(CancellationToken.None);
                    return;
                }

                try
                {
                    // Deduplicate
                    if (
                        await _emailRepository.ExistsByContentHashAsync(
                            email.ContentHash,
                            cancellationToken
                        )
                    )
                    {
                        ignoredCount++;
                        processedCount++;
                        continue;
                    }

                    // Run through Rules Engine
                    var evaluation = await _rulesEngine.EvaluateAsync(
                        email,
                        rules,
                        cancellationToken
                    );
                    email.SetDirective(evaluation);

                    if (evaluation.ShouldProcess)
                    {
                        await _emailRepository.AddAsync(email, cancellationToken);
                        savedCount++;
                    }
                    else
                    {
                        ignoredCount++;
                    }

                    processedCount++;

                    // Update progress every 5 emails
                    if (processedCount % 5 == 0)
                    {
                        job.UpdateProgress(processedCount, savedCount, ignoredCount);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        await _jobNotificationService.NotifyJobUpdated(userId, job);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error processing email {Id} for job {JobId}",
                        email.Id,
                        jobId
                    );
                    ignoredCount++;
                    processedCount++;
                }
            }

            // Update connection last sync time
            if (fetchedEmails.Count > 0)
            {
                connection.UpdateLastSync(DateTime.UtcNow, connection.HistoryId ?? string.Empty);
                _connectionRepository.Update(connection);
            }

            // Final update
            job.UpdateProgress(processedCount, savedCount, ignoredCount);
            job.Complete();

            // Complete sync history
            syncHistory.Complete(fetchedEmails.Count, savedCount, true);
            await _syncHistoryRepository.UpdateAsync(syncHistory, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Notify clients of job completion
            await _jobNotificationService.NotifyJobCompleted(userId, job);

            _logger.LogInformation(
                "Email sync job {JobId} completed: {Fetched} fetched, {Saved} saved, {Ignored} ignored",
                jobId,
                fetchedEmails.Count,
                savedCount,
                ignoredCount
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
