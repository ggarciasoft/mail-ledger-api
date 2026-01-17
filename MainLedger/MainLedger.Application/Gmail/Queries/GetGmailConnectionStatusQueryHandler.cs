using MainLedger.Contracts.Gmail;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using MediatR;

namespace MainLedger.Application.Gmail.Queries;

/// <summary>
/// Handler for getting Gmail connection status.
/// </summary>
public class GetGmailConnectionStatusQueryHandler
    : IRequestHandler<GetGmailConnectionStatusQuery, GmailConnectionStatusDto>
{
    private readonly IEmailConnectionRepository _emailConnectionRepository;
    private readonly IEmailMessageRepository _emailMessageRepository;

    public GetGmailConnectionStatusQueryHandler(
        IEmailConnectionRepository emailConnectionRepository,
        IEmailMessageRepository emailMessageRepository
    )
    {
        _emailConnectionRepository = emailConnectionRepository;
        _emailMessageRepository = emailMessageRepository;
    }

    public async Task<GmailConnectionStatusDto> Handle(
        GetGmailConnectionStatusQuery request,
        CancellationToken cancellationToken
    )
    {
        var connection = await _emailConnectionRepository.GetByUserAndProviderAsync(
            request.UserId,
            EmailProvider.Gmail
        );

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
        var totalEmails = await _emailMessageRepository.CountByUserIdAsync(
            request.UserId,
            cancellationToken
        );

        return new GmailConnectionStatusDto(
            IsConnected: true,
            Email: connection.Email,
            LastSyncedAt: connection.LastSyncedAt,
            ConnectedAt: connection.ConnectedAt,
            TotalEmailsSynced: totalEmails
        );
    }
}
