using MainLedger.Contracts.Emails;
using MainLedger.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.Emails.Queries;

public class GetEmailStatisticsQueryHandler : IRequestHandler<GetEmailStatisticsQuery, EmailStatisticsDto>
{
    private readonly IEmailMessageRepository _emailRepository;
    private readonly IEmailConnectionRepository _connectionRepository;
    private readonly ILogger<GetEmailStatisticsQueryHandler> _logger;

    public GetEmailStatisticsQueryHandler(
        IEmailMessageRepository emailRepository,
        IEmailConnectionRepository connectionRepository,
        ILogger<GetEmailStatisticsQueryHandler> logger)
    {
        _emailRepository = emailRepository;
        _connectionRepository = connectionRepository;
        _logger = logger;
    }

    public async Task<EmailStatisticsDto> Handle(GetEmailStatisticsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting email statistics for user {UserId}", request.UserId);

        var statistics = await _emailRepository.GetStatisticsAsync(request.UserId, cancellationToken);
        
        // Get last sync time from Email connection
        var connection = await _connectionRepository.GetByUserIdAsync(request.UserId);

        return new EmailStatisticsDto
        {
            TotalEmails = statistics.TotalEmails,
            Pending = statistics.Pending,
            Classified = statistics.Classified,
            Extracted = statistics.Extracted,
            Failed = statistics.Failed,
            FinancialEmails = statistics.FinancialEmails,
            NonFinancialEmails = statistics.NonFinancialEmails,
            LastSyncAt = connection.Max(o => o.LastSyncedAt)
        };
    }
}
