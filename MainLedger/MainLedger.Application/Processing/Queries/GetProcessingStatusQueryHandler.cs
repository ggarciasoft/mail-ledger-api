using MainLedger.Contracts.Processing;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.Processing.Queries;

public class GetProcessingStatusQueryHandler
    : IRequestHandler<GetProcessingStatusQuery, ProcessingStatusDto>
{
    private readonly IEmailMessageRepository _emailRepository;
    private readonly IExtractionCandidateRepository _candidateRepository;
    private readonly IProcessingJobRepository _jobRepository;
    private readonly ILogger<GetProcessingStatusQueryHandler> _logger;

    public GetProcessingStatusQueryHandler(
        IEmailMessageRepository emailRepository,
        IExtractionCandidateRepository candidateRepository,
        IProcessingJobRepository jobRepository,
        ILogger<GetProcessingStatusQueryHandler> logger
    )
    {
        _emailRepository = emailRepository;
        _candidateRepository = candidateRepository;
        _jobRepository = jobRepository;
        _logger = logger;
    }

    public async Task<ProcessingStatusDto> Handle(
        GetProcessingStatusQuery request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogInformation("Getting processing status for user {UserId}", request.UserId);

        // Get emails pending classification (limit to a reasonable number for counting)
        var pendingEmails = await _emailRepository.GetByProcessingStatusAsync(
            request.UserId,
            EmailProcessingStatus.Pending,
            1000,
            cancellationToken
        );

        // Get emails classified (ready for extraction)
        var classifiedEmails = await _emailRepository.GetClassifiedFinancialEmailsAsync(
            request.UserId,
            1000,
            cancellationToken
        );

        // Get pending extraction candidates
        var (pendingCandidates, _) = await _candidateRepository.GetPagedAsync(
            request.UserId,
            RecordStatus.Pending,
            1,
            1,
            "createdAt",
            "desc",
            cancellationToken
        );

        // Get last classification job
        var recentJobs = await _jobRepository.GetRecentJobsAsync(
            request.UserId,
            10,
            cancellationToken
        );
        var lastClassificationJob = recentJobs
            .FirstOrDefault(o => o.JobType == JobType.Classification);
        var lastExtractionJob = recentJobs
            .FirstOrDefault(o => o.JobType == JobType.Extraction);

        return new ProcessingStatusDto
        {
            PendingClassification = pendingEmails.Count,
            PendingExtraction = classifiedEmails.Count,
            CanClassify = pendingEmails.Count > 0,
            CanExtract = classifiedEmails.Count > 0,
            LastClassificationJob =
                lastClassificationJob != null
                    ? new JobStatusDto
                    {
                        Processed = lastClassificationJob.ProcessedItems,
                        Succeeded = lastClassificationJob.SuccessCount,
                        Failed = lastClassificationJob.FailureCount,
                        StartedAt =
                            lastClassificationJob.StartedAt ?? lastClassificationJob.CreatedAt,
                        CompletedAt = lastClassificationJob.CompletedAt,
                    }
                    : null,
            LastExtractionJob =
                lastExtractionJob != null
                    ? new JobStatusDto
                    {
                        Processed = lastExtractionJob.ProcessedItems,
                        Succeeded = lastExtractionJob.SuccessCount,
                        Failed = lastExtractionJob.FailureCount,
                        StartedAt = lastExtractionJob.StartedAt ?? lastExtractionJob.CreatedAt,
                        CompletedAt = lastExtractionJob.CompletedAt,
                    }
                    : null,
        };
    }
}
