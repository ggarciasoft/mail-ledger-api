using MainLedger.Contracts.Gmail;
using MainLedger.Domain.Repositories;
using MediatR;

namespace MainLedger.Application.Gmail.Queries;

/// <summary>
/// Handler for getting Gmail connection status.
/// </summary>
public class GetGmailConnectionStatusQueryHandler : IRequestHandler<GetGmailConnectionStatusQuery, GmailConnectionStatusDto>
{
    private readonly IGmailConnectionRepository _gmailConnectionRepository;
    private readonly IEmailMessageRepository _emailMessageRepository;

    public GetGmailConnectionStatusQueryHandler(
        IGmailConnectionRepository gmailConnectionRepository,
        IEmailMessageRepository emailMessageRepository)
    {
        _gmailConnectionRepository = gmailConnectionRepository;
        _emailMessageRepository = emailMessageRepository;
    }

    public async Task<GmailConnectionStatusDto> Handle(GetGmailConnectionStatusQuery request, CancellationToken cancellationToken)
    {
        var connection = await _gmailConnectionRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        if (connection == null || !connection.IsActive)
        {
            return new GmailConnectionStatusDto(
                IsConnected: false,
                Email: null,
                LastSyncedAt: null,
                ConnectedAt: null,
                TotalEmailsSynced: 0
            );
        }

        // Get total emails synced for this user
        var totalEmails = await _emailMessageRepository.CountByUserIdAsync(request.UserId, cancellationToken);

        return new GmailConnectionStatusDto(
            IsConnected: true,
            Email: connection.Email.ToString(),
            LastSyncedAt: connection.LastSyncedAt,
            ConnectedAt: connection.CreatedAt,
            TotalEmailsSynced: totalEmails
        );
    }
}
