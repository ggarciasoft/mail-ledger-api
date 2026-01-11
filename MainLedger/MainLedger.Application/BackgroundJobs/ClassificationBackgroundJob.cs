using Hangfire;
using MainLedger.Application.Common.Interfaces;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.BackgroundJobs;

/// <summary>
/// Background job for classifying emails.
/// </summary>
public class ClassificationBackgroundJob
{
    private readonly IEmailMessageRepository _emailRepository;
    private readonly IClassificationService _classificationService;
    private readonly IProcessingJobRepository _jobRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ClassificationBackgroundJob> _logger;

    public ClassificationBackgroundJob(
        IEmailMessageRepository emailRepository,
        IClassificationService classificationService,
        IProcessingJobRepository jobRepository,
        IUnitOfWork unitOfWork,
        ILogger<ClassificationBackgroundJob> logger
    )
    {
        _emailRepository = emailRepository;
        _classificationService = classificationService;
        _jobRepository = jobRepository;
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
                "Starting classification job {JobId} for user {UserId}",
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

            foreach (var email in emails)
            {
                try
                {
                    var result = await _classificationService.ClassifyEmailAsync(
                        email,
                        cancellationToken
                    );

                    // Set classification based on result
                    email.SetClassification(result.IsFinancial, result.Category, result.Confidence);

                    // Update processing status to Classified
                    email.SetProcessingStatus(EmailProcessingStatus.Classified);

                    _emailRepository.Update(email);
                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error classifying email {EmailId}", email.Id);
                    failureCount++;
                }

                processedCount++;

                // Update progress every 5 emails
                if (processedCount % 5 == 0)
                {
                    job.UpdateProgress(processedCount, successCount, failureCount);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }

            // Final update
            job.UpdateProgress(processedCount, successCount, failureCount);
            job.Complete();
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Classification job {JobId} completed: {Success} success, {Failure} failures",
                jobId,
                successCount,
                failureCount
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Classification job {JobId} failed", jobId);
            job.Fail(ex.Message);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw;
        }
    }
}
