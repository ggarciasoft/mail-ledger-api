using Hangfire;
using MainLedger.Application.Common.Interfaces;
using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.BackgroundJobs;

/// <summary>
/// Background job for extracting financial data from classified emails using parallel processing.
/// </summary>
public class ExtractionBackgroundJob
{
    private readonly IJobNotificationService _jobNotificationService;
    private readonly ILogger<ExtractionBackgroundJob> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ExtractionBackgroundJob(
        IJobNotificationService jobNotificationService,
        ILogger<ExtractionBackgroundJob> logger,
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
                "Starting extraction job {JobId} for user {UserId} with parallel processing",
                jobId,
                userId
            );

            // Get active extraction version and emails using scoped services
            ExtractionVersion version;
            List<EmailMessage> emails;
            using (var scope = _serviceProvider.CreateScope())
            {
                var versionRepository =
                    scope.ServiceProvider.GetRequiredService<IExtractionVersionRepository>();
                var emailRepository =
                    scope.ServiceProvider.GetRequiredService<IEmailMessageRepository>();

                version = await versionRepository.GetActiveAsync(cancellationToken);
                if (version == null)
                {
                    throw new InvalidOperationException("No active extraction version found");
                }

                emails = await emailRepository.GetPendingExtractionAsync(
                    userId,
                    batchSize,
                    cancellationToken
                );
            }

            int successCount = 0;
            int failureCount = 0;
            int processedCount = 0;
            int lastReportedCount = 0;

            // Use semaphore to limit concurrent OpenAI API calls (max 5 concurrent for extraction)
            var semaphore = new SemaphoreSlim(5);

            // Create tasks for parallel processing - each with its own scope
            var tasks = emails.Select(async email =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    // Create a new scope for this parallel task
                    using var scope = _serviceProvider.CreateScope();
                    var extractionService =
                        scope.ServiceProvider.GetRequiredService<IExtractionService>();
                    var normalizationService =
                        scope.ServiceProvider.GetRequiredService<INormalizationService>();
                    var jobRepository =
                        scope.ServiceProvider.GetRequiredService<IProcessingJobRepository>();

                    // Check if job has been cancelled
                    var currentJob = await jobRepository.GetByIdAsync(jobId, cancellationToken);
                    if (currentJob?.Status == Domain.Enums.JobStatus.Cancelled)
                    {
                        return (
                            email,
                            candidate: (ExtractionCandidate?)null,
                            error: (Exception?)null,
                            cancelled: true
                        );
                    }

                    // Check cancellation token
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return (
                            email,
                            candidate: (ExtractionCandidate?)null,
                            error: (Exception?)null,
                            cancelled: true
                        );
                    }

                    try
                    {
                        // Extract financial data
                        var extractionResult = await extractionService.ExtractFinancialDataAsync(
                            email,
                            cancellationToken
                        );

                        // Normalize the extracted data
                        var normalizationResult =
                            await normalizationService.NormalizeExtractionAsync(
                                extractionResult,
                                email,
                                cancellationToken
                            );

                        // Skip if normalization failed
                        if (!normalizationResult.IsValid)
                        {
                            _logger.LogWarning(
                                "Normalization failed for email {EmailId}: {Errors}",
                                email.Id,
                                string.Join(", ", normalizationResult.Errors)
                            );
                            return (
                                email,
                                candidate: (ExtractionCandidate?)null,
                                error: new Exception("Normalization failed"),
                                cancelled: false
                            );
                        }

                        // Create extraction candidate
                        var candidate = ExtractionCandidate.Create(email.Id, version.Id);

                        // Set transaction data with normalized values
                        Domain.ValueObjects.Money? amount = null;
                        if (
                            normalizationResult.NormalizedAmount.HasValue
                            && !string.IsNullOrWhiteSpace(normalizationResult.NormalizedCurrency)
                        )
                        {
                            if (
                                Enum.TryParse<Domain.Enums.Currency>(
                                    normalizationResult.NormalizedCurrency,
                                    true,
                                    out var currency
                                )
                            )
                            {
                                amount = Domain.ValueObjects.Money.Create(
                                    normalizationResult.NormalizedAmount.Value,
                                    currency
                                );
                            }
                        }

                        candidate.SetTransactionData(
                            amount,
                            normalizationResult.NormalizedDate,
                            normalizationResult.NormalizedMerchant,
                            normalizationResult.AmountConfidence > 0
                                ? Domain.ValueObjects.Confidence.Create(
                                    normalizationResult.AmountConfidence
                                )
                                : null,
                            normalizationResult.DateConfidence > 0
                                ? Domain.ValueObjects.Confidence.Create(
                                    normalizationResult.DateConfidence
                                )
                                : null,
                            normalizationResult.MerchantConfidence > 0
                                ? Domain.ValueObjects.Confidence.Create(
                                    normalizationResult.MerchantConfidence
                                )
                                : null
                        );

                        // Set account info
                        Domain.ValueObjects.AccountNumber? sourceAccount = null;
                        Domain.ValueObjects.AccountNumber? targetAccount = null;

                        if (!string.IsNullOrWhiteSpace(normalizationResult.NormalizedSourceAccount))
                        {
                            sourceAccount = Domain.ValueObjects.AccountNumber.Create(
                                normalizationResult.NormalizedSourceAccount
                            );
                        }

                        if (!string.IsNullOrWhiteSpace(normalizationResult.NormalizedTargetAccount))
                        {
                            targetAccount = Domain.ValueObjects.AccountNumber.Create(
                                normalizationResult.NormalizedTargetAccount
                            );
                        }

                        Domain.ValueObjects.BankProvider? sourceBank = null;
                        Domain.ValueObjects.BankProvider? targetBank = null;

                        if (!string.IsNullOrWhiteSpace(normalizationResult.NormalizedSourceBank))
                        {
                            sourceBank = Domain.ValueObjects.BankProvider.Create(
                                normalizationResult.NormalizedSourceBank,
                                normalizationResult.NormalizedSourceBank
                            );
                        }

                        if (!string.IsNullOrWhiteSpace(normalizationResult.NormalizedTargetBank))
                        {
                            targetBank = Domain.ValueObjects.BankProvider.Create(
                                normalizationResult.NormalizedTargetBank,
                                normalizationResult.NormalizedTargetBank
                            );
                        }

                        candidate.SetAccountInfo(
                            sourceAccount,
                            targetAccount,
                            sourceBank,
                            targetBank
                        );

                        // Set additional details
                        Domain.ValueObjects.Money? fees = null;
                        Domain.ValueObjects.Money? tax = null;

                        if (normalizationResult.NormalizedFees.HasValue && amount != null)
                        {
                            fees = Domain.ValueObjects.Money.Create(
                                normalizationResult.NormalizedFees.Value,
                                amount.Currency
                            );
                        }

                        if (normalizationResult.NormalizedTax.HasValue && amount != null)
                        {
                            tax = Domain.ValueObjects.Money.Create(
                                normalizationResult.NormalizedTax.Value,
                                amount.Currency
                            );
                        }

                        candidate.SetAdditionalDetails(fees, tax, normalizationResult.ReferenceId);

                        return (email, candidate, error: (Exception?)null, cancelled: false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Error extracting data from email {EmailId}",
                            email.Id
                        );
                        return (
                            email,
                            candidate: (ExtractionCandidate?)null,
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
                _logger.LogInformation("Job {JobId} was cancelled during extraction", jobId);
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
                var candidateRepository =
                    scope.ServiceProvider.GetRequiredService<IExtractionCandidateRepository>();
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

                foreach (var (email, candidate, error, _) in results)
                {
                    if (error != null)
                    {
                        failureCount++;
                    }
                    else if (candidate != null)
                    {
                        await candidateRepository.AddAsync(candidate, cancellationToken);

                        // Update email processing status
                        email.SetProcessingStatus(EmailProcessingStatus.Extracted);
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

                // Mark job as complete and save in same scope
                trackedJob.Complete();
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            await _jobNotificationService.NotifyJobUpdated(userId, job);
            await _jobNotificationService.NotifyJobCompleted(userId, job);

            _logger.LogInformation(
                "Extraction job {JobId} completed with parallel processing. Processed: {ProcessedCount}, Success: {SuccessCount}, Failures: {FailureCount}",
                jobId,
                processedCount,
                successCount,
                failureCount
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Extraction job {JobId} failed", jobId);
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
