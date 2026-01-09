using MainLedger.Contracts.Gmail;
using MainLedger.Domain.Repositories;
using MediatR;

namespace MainLedger.Application.Gmail.Queries;

/// <summary>
/// Handler for getting sync history.
/// </summary>
public class GetSyncHistoryQueryHandler : IRequestHandler<GetSyncHistoryQuery, SyncHistoryDto>
{
    private readonly IEmailMessageRepository _emailMessageRepository;
    private readonly IGmailConnectionRepository _gmailConnectionRepository;

    public GetSyncHistoryQueryHandler(
        IEmailMessageRepository emailMessageRepository,
        IGmailConnectionRepository gmailConnectionRepository)
    {
        _emailMessageRepository = emailMessageRepository;
        _gmailConnectionRepository = gmailConnectionRepository;
    }

    public async Task<SyncHistoryDto> Handle(GetSyncHistoryQuery request, CancellationToken cancellationToken)
    {
        var connection = await _gmailConnectionRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        // Get sync history by grouping emails by the date they were created (ingested)
        var syncHistory = await _emailMessageRepository.GetSyncHistoryAsync(request.UserId, request.Limit, cancellationToken);

        return new SyncHistoryDto(
            History: syncHistory.Select(s => new SyncHistoryItemDto(
                SyncedAt: s.SyncedAt,
                EmailsProcessed: s.EmailCount,
                Status: "Completed"
            )).ToList(),
            LastSuccessfulSync: connection?.LastSyncedAt,
            TotalSyncs: syncHistory.Count
        );
    }
}
