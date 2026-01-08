using MainLedger.Contracts.Processing;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.Processing.Queries;

public class GetProcessingStatusQueryHandler : IRequestHandler<GetProcessingStatusQuery, ProcessingStatusDto>
{
    private readonly IEmailMessageRepository _emailRepository;
    private readonly IExtractionCandidateRepository _candidateRepository;
    private readonly ILogger<GetProcessingStatusQueryHandler> _logger;

    public GetProcessingStatusQueryHandler(
        IEmailMessageRepository emailRepository,
        IExtractionCandidateRepository candidateRepository,
        ILogger<GetProcessingStatusQueryHandler> logger)
    {
        _emailRepository = emailRepository;
        _candidateRepository = candidateRepository;
        _logger = logger;
    }

    public async Task<ProcessingStatusDto> Handle(GetProcessingStatusQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting processing status for user {UserId}", request.UserId);

        // Get emails pending classification (limit to a reasonable number for counting)
        var pendingEmails = await _emailRepository.GetByProcessingStatusAsync(
            request.UserId, EmailProcessingStatus.Pending, 1000, cancellationToken);

        // Get emails classified (ready for extraction)
        var classifiedEmails = await _emailRepository.GetClassifiedFinancialEmailsAsync(
            request.UserId, 1000, cancellationToken);

        // Get pending extraction candidates
        var (pendingCandidates, _) = await _candidateRepository.GetPagedAsync(
            request.UserId, RecordStatus.Pending, 1, 1, "createdAt", "desc", cancellationToken);

        return new ProcessingStatusDto
        {
            PendingClassification = pendingEmails.Count,
            PendingExtraction = classifiedEmails.Count,
            CanClassify = pendingEmails.Count > 0,
            CanExtract = classifiedEmails.Count > 0,
            LastClassificationJob = null, // Could be enhanced with job tracking
            LastExtractionJob = null // Could be enhanced with job tracking
        };
    }
}
