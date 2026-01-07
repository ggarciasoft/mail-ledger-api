using MainLedger.Application.Common.Interfaces;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.Emails.Commands;

/// <summary>
/// Command to batch classify pending emails using AI.
/// Processes up to a specified number of emails in parallel.
/// </summary>
public record BatchClassifyEmailsCommand(Guid UserId, int BatchSize = 20) : IRequest<BatchClassificationResult>;

public record BatchClassificationResult
{
    public int EmailsProcessed { get; init; }
    public int EmailsClassified { get; init; }
    public int EmailsFailed { get; init; }
    public string Status { get; init; } = string.Empty;
}

public class BatchClassifyEmailsCommandHandler : IRequestHandler<BatchClassifyEmailsCommand, BatchClassificationResult>
{
    private readonly IEmailMessageRepository _emailRepository;
    private readonly IClassificationService _classificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BatchClassifyEmailsCommandHandler> _logger;

    public BatchClassifyEmailsCommandHandler(
        IEmailMessageRepository emailRepository,
        IClassificationService classificationService,
        IUnitOfWork unitOfWork,
        ILogger<BatchClassifyEmailsCommandHandler> logger)
    {
        _emailRepository = emailRepository;
        _classificationService = classificationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<BatchClassificationResult> Handle(BatchClassifyEmailsCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting batch classification for user {UserId} with batch size {BatchSize}",
            request.UserId, request.BatchSize);

        // Fetch pending emails for this user
        var pendingEmails = await _emailRepository.GetByProcessingStatusAsync(
            request.UserId,
            EmailProcessingStatus.Pending,
            request.BatchSize,
            cancellationToken);

        if (!pendingEmails.Any())
        {
            _logger.LogInformation("No pending emails found for user {UserId}", request.UserId);
            return new BatchClassificationResult
            {
                EmailsProcessed = 0,
                EmailsClassified = 0,
                EmailsFailed = 0,
                Status = "No pending emails"
            };
        }

        int classifiedCount = 0;
        int failedCount = 0;

        // Process emails in parallel (with controlled concurrency)
        var classificationTasks = pendingEmails.Select(async email =>
        {
            try
            {
                _logger.LogDebug("Classifying email {MessageId}", email.MessageId);

                var classification = await _classificationService.ClassifyEmailAsync(email, cancellationToken);
                
                email.SetClassification(
                    classification.IsFinancial,
                    classification.Category,
                    classification.Confidence);

                email.SetProcessingStatus(EmailProcessingStatus.Classified);

                _logger.LogInformation(
                    "Email {MessageId} classified: IsFinancial={IsFinancial}, Category={Category}",
                    email.MessageId, classification.IsFinancial, classification.Category);

                Interlocked.Increment(ref classifiedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Classification failed for email {MessageId}", email.MessageId);
                email.SetProcessingStatus(EmailProcessingStatus.Failed, ex.Message);
                Interlocked.Increment(ref failedCount);
            }
        });

        // Wait for all classifications to complete
        await Task.WhenAll(classificationTasks);

        // Save all changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Batch classification completed: {Processed} processed, {Classified} classified, {Failed} failed",
            pendingEmails.Count, classifiedCount, failedCount);

        return new BatchClassificationResult
        {
            EmailsProcessed = pendingEmails.Count,
            EmailsClassified = classifiedCount,
            EmailsFailed = failedCount,
            Status = "Success"
        };
    }
}
