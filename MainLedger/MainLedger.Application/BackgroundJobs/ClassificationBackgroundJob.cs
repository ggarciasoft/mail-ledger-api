using Hangfire;
using MainLedger.Application.Common.Interfaces;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.BackgroundJobs;

/// <summary>
/// Background job for classifying emails using parallel processing.
/// </summary>
public class ClassificationBackgroundJob
{
    private readonly IJobNotificationService _jobNotificationService;
    private readonly ILogger<ClassificationBackgroundJob> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ClassificationBackgroundJob(
        IJobNotificationService jobNotificationService,
        ILogger<ClassificationBackgroundJob> logger,
        IServiceProvider serviceProvider
    )
    {
        _jobNotificationService = jobNotificationService;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync(
        Guid jobId,
        Guid userId,
        int batchSize,
        CancellationToken cancellationToken
    )
    {
        // Fetch job using scoped repository
        Domain.Entities.ProcessingJob job;
        using (var scope = _serviceProvider.CreateScope())
        {
            var jobRepository =
                scope.ServiceProvider.GetRequiredService<IProcessingJobRepository>();
            job = await jobRepository.GetByIdAsync(jobId, cancellationToken);
        }

        if (job == null)
        {
            _logger.LogError("Job {JobId} not found", jobId);
            return;
        }

        try
        {
            _logger.LogInformation(
                "Starting classification job {JobId} for user {UserId} with parallel processing",
                jobId,
                userId
            );

            // Check subscription limits before fetching emails
            int remainingEmails;
            using (var scope = _serviceProvider.CreateScope())
            {
                var subscriptionService =
                    scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
                var (emailsProcessed, emailLimit) = await subscriptionService.GetEmailUsageAsync(
                    userId,
                    cancellationToken
                );
                remainingEmails = emailLimit - emailsProcessed;

                if (remainingEmails <= 0)
                {
                    _logger.LogWarning(
                        "User {UserId} has reached their email limit ({EmailLimit}). Skipping classification.",
                        userId,
                        emailLimit
                    );
                    job.Fail("Monthly email limit reached. Please upgrade your subscription.");
                    using (var saveScope = _serviceProvider.CreateScope())
                    {
                        var unitOfWork =
                            saveScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                        await unitOfWork.SaveChangesAsync(cancellationToken);
                    }
                    await _jobNotificationService.NotifyJobFailed(userId, job);
                    return;
                }
            }

            // Limit batch size to remaining emails
            var effectiveBatchSize = Math.Min(batchSize, remainingEmails);

            // Get pending emails using a scoped repository
            List<Domain.Entities.EmailMessage> emails;
            using (var scope = _serviceProvider.CreateScope())
            {
                var emailRepository =
                    scope.ServiceProvider.GetRequiredService<IEmailMessageRepository>();
                emails = await emailRepository.GetPendingClassificationAsync(
                    userId,
                    effectiveBatchSize,
                    cancellationToken
                );
            }

            int successCount = 0;
            int failureCount = 0;
            int skippedCount = 0;
            int processedCount = 0;
            int lastReportedCount = 0;

            // Use semaphore to limit concurrent OpenAI API calls (max 10 concurrent)
            var semaphore = new SemaphoreSlim(10);

            // Create tasks for parallel processing - each with its own scope
            var tasks = emails.Select(async email =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    // Create a new scope for this parallel task
                    using var scope = _serviceProvider.CreateScope();
                    var classificationService =
                        scope.ServiceProvider.GetRequiredService<IClassificationService>();
                    var emailRepository =
                        scope.ServiceProvider.GetRequiredService<IEmailMessageRepository>();
                    var jobRepository =
                        scope.ServiceProvider.GetRequiredService<IProcessingJobRepository>();
                    var subscriptionService =
                        scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                    // Check if job has been cancelled
                    var currentJob = await jobRepository.GetByIdAsync(jobId, cancellationToken);
                    if (currentJob?.Status == Domain.Enums.JobStatus.Cancelled)
                    {
                        return (
                            email,
                            result: null,
                            error: (Exception?)null,
                            cancelled: true,
                            limitReached: false
                        );
                    }

                    // Check cancellation token
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return (
                            email,
                            result: null,
                            error: (Exception?)null,
                            cancelled: true,
                            limitReached: false
                        );
                    }

                    // Check subscription limit before processing
                    var canProcess = await subscriptionService.CanProcessEmailAsync(
                        userId,
                        cancellationToken
                    );
                    if (!canProcess)
                    {
                        _logger.LogWarning(
                            "User {UserId} has reached their email limit. Skipping email {EmailId}",
                            userId,
                            email.Id
                        );
                        return (
                            email,
                            result: null,
                            error: (Exception?)null,
                            cancelled: false,
                            limitReached: true
                        );
                    }

                    try
                    {
                        var result = await classificationService.ClassifyEmailAsync(
                            email,
                            cancellationToken
                        );

                        // Don't increment here - will do it in bulk after all processing to avoid race conditions
                        return (
                            email,
                            result,
                            error: (Exception?)null,
                            cancelled: false,
                            limitReached: false
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error classifying email {EmailId}", email.Id);
                        return (
                            email,
                            result: null,
                            error: ex,
                            cancelled: false,
                            limitReached: false
                        );
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            });

            // Wait for all tasks to complete
            var results = await Task.WhenAll(tasks);

            // Check if job was cancelled during processing
            if (results.Any(r => r.cancelled))
            {
                _logger.LogInformation("Job {JobId} was cancelled during classification", jobId);
                job.Cancel();
                using (var scope = _serviceProvider.CreateScope())
                {
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    await unitOfWork.SaveChangesAsync(CancellationToken.None);
                }
                return;
            }

            // Process results and save using scoped services
            using (var scope = _serviceProvider.CreateScope())
            {
                var emailRepository =
                    scope.ServiceProvider.GetRequiredService<IEmailMessageRepository>();
                var jobRepository =
                    scope.ServiceProvider.GetRequiredService<IProcessingJobRepository>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                // Re-fetch job in this scope to ensure proper tracking
                var trackedJob = await jobRepository.GetByIdAsync(jobId, cancellationToken);

                // Start the job with the email count
                trackedJob.Start(emails.Count);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                foreach (var (email, result, error, _, limitReached) in results)
                {
                    if (limitReached)
                    {
                        skippedCount++;
                        _logger.LogInformation(
                            "Email {EmailId} skipped due to subscription limit",
                            email.Id
                        );
                        continue;
                    }

                    if (error != null)
                    {
                        failureCount++;
                    }
                    else if (result != null)
                    {
                        // Set classification based on result
                        email.SetClassification(
                            result.IsFinancial,
                            result.Category,
                            result.Confidence
                        );

                        // Update processing status to Classified
                        email.SetProcessingStatus(EmailProcessingStatus.Classified);

                        emailRepository.Update(email);
                        successCount++;
                    }

                    processedCount++;

                    // Update progress every 5 emails
                    if (processedCount - lastReportedCount >= 5)
                    {
                        trackedJob.UpdateProgress(processedCount, successCount, failureCount);
                        await unitOfWork.SaveChangesAsync(cancellationToken);
                        await _jobNotificationService.NotifyJobUpdated(userId, trackedJob);
                        lastReportedCount = processedCount;
                    }
                }

                // Final progress update
                trackedJob.UpdateProgress(processedCount, successCount, failureCount);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                // Increment email count for all successfully classified emails (bulk operation to avoid race conditions)
                if (successCount > 0)
                {
                    var subscriptionService =
                        scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
                    for (int i = 0; i < successCount; i++)
                    {
                        await subscriptionService.IncrementClassificationCountAsync(
                            userId,
                            cancellationToken
                        );
                    }
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation(
                        "Incremented email count by {SuccessCount} for user {UserId}",
                        successCount,
                        userId
                    );
                }

                // Mark job as complete and save in same scope
                trackedJob.Complete();
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            await _jobNotificationService.NotifyJobUpdated(userId, job);
            await _jobNotificationService.NotifyJobCompleted(userId, job);

            _logger.LogInformation(
                "Classification job {JobId} completed with parallel processing. Processed: {ProcessedCount}, Success: {SuccessCount}, Failures: {FailureCount}, Skipped (limit): {SkippedCount}",
                jobId,
                processedCount,
                successCount,
                failureCount,
                skippedCount
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Classification job {JobId} failed", jobId);
            job.Fail(ex.Message);
            using (var scope = _serviceProvider.CreateScope())
            {
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                await unitOfWork.SaveChangesAsync(CancellationToken.None);
            }
            await _jobNotificationService.NotifyJobFailed(userId, job);
        }
    }
}
