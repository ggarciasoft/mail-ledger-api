using Hangfire;
using MainLedger.Application.Common.Interfaces;
using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.BackgroundJobs;

/// <summary>
/// Background job for extracting financial data from classified emails using parallel processing.
/// </summary>
public class ExtractionBackgroundJob
{
    private readonly IEmailMessageRepository _emailRepository;
    private readonly IExtractionCandidateRepository _candidateRepository;
    private readonly IExtractionService _extractionService;
    private readonly INormalizationService _normalizationService;
    private readonly IExtractionVersionRepository _versionRepository;
    private readonly IProcessingJobRepository _jobRepository;
    private readonly IJobNotificationService _jobNotificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ExtractionBackgroundJob> _logger;

    public ExtractionBackgroundJob(
        IEmailMessageRepository emailRepository,
        IExtractionCandidateRepository candidateRepository,
        IExtractionService extractionService,
        INormalizationService normalizationService,
        IExtractionVersionRepository versionRepository,
        IProcessingJobRepository jobRepository,
        IJobNotificationService jobNotificationService,
        IUnitOfWork unitOfWork,
        ILogger<ExtractionBackgroundJob> logger
    )
    {
        _emailRepository = emailRepository;
        _candidateRepository = candidateRepository;
        _extractionService = extractionService;
        _normalizationService = normalizationService;
        _versionRepository = versionRepository;
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
                "Starting extraction job {JobId} for user {UserId} with parallel processing",
                jobId,
                userId
            );

            // Get active extraction version
            var version = await _versionRepository.GetActiveAsync(cancellationToken);
            if (version == null)
            {
                throw new InvalidOperationException("No active extraction version found");
            }

            // Get classified emails
            var emails = await _emailRepository.GetPendingExtractionAsync(
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

            // Use semaphore to limit concurrent OpenAI API calls (max 5 concurrent for extraction)
            var semaphore = new SemaphoreSlim(5);

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
                        var extractionResult = await _extractionService.ExtractFinancialDataAsync(
                            email,
                            cancellationToken
                        );

                        // Normalize the extracted data
                        var normalizationResult =
                            await _normalizationService.NormalizeExtractionAsync(
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
                await _unitOfWork.SaveChangesAsync(CancellationToken.None);
                return;
            }

            // Process results
            foreach (var (email, candidate, error, _) in results)
            {
                if (error != null)
                {
                    failureCount++;
                }
                else if (candidate != null)
                {
                    await _candidateRepository.AddAsync(candidate, cancellationToken);

                    // Update email processing status
                    email.SetProcessingStatus(EmailProcessingStatus.Extracted);
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
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);
            await _jobNotificationService.NotifyJobFailed(userId, job);
        }
    }
}
