using Hangfire;
using MainLedger.Application.Common.Interfaces;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.BackgroundJobs;

/// <summary>
/// Background job for classifying emails using parallel processing.
/// </summary>
public class ClassificationBackgroundJob
{
    private readonly IEmailMessageRepository _emailRepository;
    private readonly IClassificationService _classificationService;
    private readonly IProcessingJobRepository _jobRepository;
    private readonly IJobNotificationService _jobNotificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ClassificationBackgroundJob> _logger;

    public ClassificationBackgroundJob(
        IEmailMessageRepository emailRepository,
        IClassificationService classificationService,
        IProcessingJobRepository jobRepository,
        IJobNotificationService jobNotificationService,
        IUnitOfWork unitOfWork,
        ILogger<ClassificationBackgroundJob> logger
    )
    {
        _emailRepository = emailRepository;
        _classificationService = classificationService;
        _jobRepository = jobRepository;
        _jobNotificationService = jobNotificationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync(
        Guid jobId,
        Guid userId,
        int batchSize,
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
                "Starting classification job {JobId} for user {UserId} with parallel processing",
                jobId,
                userId
            );

            // Get pending emails
            var emails = await _emailRepository.GetPendingClassificationAsync(
                userId,
                batchSize,
                cancellationToken
            );

            job.Start(emails.Count);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            int successCount = 0;
            int failureCount = 0;
            int processedCount = 0;
            int lastReportedCount = 0;

            // Use semaphore to limit concurrent OpenAI API calls (max 10 concurrent)
            var semaphore = new SemaphoreSlim(10);

            // Create tasks for parallel processing
            var tasks = emails.Select(async email =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    // Check if job has been cancelled
                    var currentJob = await _jobRepository.GetByIdAsync(jobId, cancellationToken);
                    if (currentJob?.Status == Domain.Enums.JobStatus.Cancelled)
                    {
                        return (
                            email,
                            result: null,
                            error: (Exception?)null,
                            cancelled: true
                        );
                    }

                    // Check cancellation token
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return (
                            email,
                            result: null,
                            error: (Exception?)null,
                            cancelled: true
                        );
                    }

                    try
                    {
                        var result = await _classificationService.ClassifyEmailAsync(
                            email,
                            cancellationToken
                        );

                        return (email, result, error: (Exception?)null, cancelled: false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error classifying email {EmailId}", email.Id);
                        return (
                            email,
                            result:null,
                            error: ex,
                            cancelled: false
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
                await _unitOfWork.SaveChangesAsync(CancellationToken.None);
                return;
            }

            // Process results
            foreach (var (email, result, error, _) in results)
            {
                if (error != null)
                {
                    failureCount++;
                }
                else if (result != null)
                {
                    // Set classification based on result
                    email.SetClassification(result.IsFinancial, result.Category, result.Confidence);

                    // Update processing status to Classified
                    email.SetProcessingStatus(EmailProcessingStatus.Classified);

                    _emailRepository.Update(email);
                    successCount++;
                }

                processedCount++;

                // Update progress every 5 emails
                if (processedCount - lastReportedCount >= 5)
                {
                    job.UpdateProgress(processedCount, successCount, failureCount);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    await _jobNotificationService.NotifyJobUpdated(userId, job);
                    lastReportedCount = processedCount;
                }
            }

            // Final progress update
            job.UpdateProgress(processedCount, successCount, failureCount);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _jobNotificationService.NotifyJobUpdated(userId, job);

            // Mark job as complete
            job.Complete();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _jobNotificationService.NotifyJobCompleted(userId, job);

            _logger.LogInformation(
                "Classification job {JobId} completed with parallel processing. Processed: {ProcessedCount}, Success: {SuccessCount}, Failures: {FailureCount}",
                jobId,
                processedCount,
                successCount,
                failureCount
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Classification job {JobId} failed", jobId);
            job.Fail(ex.Message);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);
            await _jobNotificationService.NotifyJobFailed(userId, job);
        }
    }
}
