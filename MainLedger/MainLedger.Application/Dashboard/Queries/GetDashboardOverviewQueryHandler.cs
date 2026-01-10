using MainLedger.Contracts.Dashboard;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.Dashboard.Queries;

public class GetDashboardOverviewQueryHandler
    : IRequestHandler<GetDashboardOverviewQuery, DashboardOverviewDto>
{
    private readonly IEmailMessageRepository _emailRepository;
    private readonly IExtractionCandidateRepository _candidateRepository;
    private readonly IFinancialRecordRepository _recordRepository;
    private readonly IGmailConnectionRepository _gmailRepository;
    private readonly ILogger<GetDashboardOverviewQueryHandler> _logger;

    public GetDashboardOverviewQueryHandler(
        IEmailMessageRepository emailRepository,
        IExtractionCandidateRepository candidateRepository,
        IFinancialRecordRepository recordRepository,
        IGmailConnectionRepository gmailRepository,
        ILogger<GetDashboardOverviewQueryHandler> logger
    )
    {
        _emailRepository = emailRepository;
        _candidateRepository = candidateRepository;
        _recordRepository = recordRepository;
        _gmailRepository = gmailRepository;
        _logger = logger;
    }

    public async Task<DashboardOverviewDto> Handle(
        GetDashboardOverviewQuery request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogInformation("Getting dashboard overview for user {UserId}", request.UserId);

        // Get email statistics
        var emailStats = await _emailRepository.GetStatisticsAsync(
            request.UserId,
            cancellationToken
        );

        // Get extraction candidates by status
        var (pendingCandidates, _) = await _candidateRepository.GetPagedAsync(
            request.UserId,
            RecordStatus.Pending,
            1,
            1,
            "createdAt",
            "desc",
            cancellationToken
        );
        var pendingCount = pendingCandidates.Count;

        // Get confirmed financial records
        var confirmedRecords = await _recordRepository.GetByUserIdAsync(
            request.UserId,
            RecordStatus.Confirmed,
            cancellationToken
        );

        // Calculate financial metrics
        var totalSpending = confirmedRecords.Sum(r => r.Amount.Amount);
        var avgTransaction = confirmedRecords.Any()
            ? confirmedRecords.Average(r => r.Amount.Amount)
            : 0;

        // Get last sync time
        var gmailConnection = await _gmailRepository.GetByUserIdAsync(
            request.UserId,
            cancellationToken
        );
        var lastSyncAt = gmailConnection?.LastSyncedAt;

        // Build recent activity (simplified - could be enhanced with actual activity tracking)
        var recentActivity = new List<RecentActivityDto>();

        if (lastSyncAt.HasValue)
        {
            recentActivity.Add(
                new RecentActivityDto
                {
                    Type = "EmailSynced",
                    Count = emailStats.TotalEmails,
                    Timestamp = lastSyncAt.Value,
                }
            );
        }

        return new DashboardOverviewDto
        {
            TotalEmails = emailStats.TotalEmails,
            PendingClassification = emailStats.Pending,
            PendingExtraction = emailStats.Classified,
            PendingConfirmation = pendingCount,
            ConfirmedRecords = confirmedRecords.Count,
            FailedProcessing = emailStats.Failed,
            TotalSpending = totalSpending,
            AvgTransaction = avgTransaction,
            LastSyncAt = lastSyncAt,
            RecentActivity = recentActivity,
        };
    }
}
