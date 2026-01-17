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
    private readonly IEmailConnectionRepository _emailConnectionRepository;

    public GetSyncHistoryQueryHandler(
        IEmailMessageRepository emailMessageRepository,
        IEmailConnectionRepository emailConnectionRepository)
    {
        _emailMessageRepository = emailMessageRepository;
        _emailConnectionRepository = emailConnectionRepository;
    }

    public async Task<SyncHistoryDto> Handle(GetSyncHistoryQuery request, CancellationToken cancellationToken)
    {
        var connection = await _emailConnectionRepository.GetByUserIdAsync(request.UserId);

        // Get sync history by grouping emails by the date they were created (ingested)
        var syncHistory = await _emailMessageRepository.GetSyncHistoryAsync(request.UserId, request.Limit, cancellationToken);

        return new SyncHistoryDto(
            History: syncHistory.Select(s => new SyncHistoryItemDto(
                SyncedAt: s.SyncedAt,
                EmailsProcessed: s.EmailCount,
                Status: "Completed"
            )).ToList(),
            LastSuccessfulSync: connection.Max(o => o.LastSyncedAt),
            TotalSyncs: syncHistory.Count
        );
    }
}
