using MainLedger.Application.Common.Interfaces;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.Emails.Commands;

/// <summary>
/// Command to sync emails from Gmail for a specific user.
/// Fetches new emails and persists them to the database.
/// </summary>
public record SyncGmailEmailsCommand(Guid UserId) : IRequest<SyncResult>;

public record SyncResult
{
    public int EmailsFetched { get; init; }
    public int EmailsSaved { get; init; }
    public int EmailsIgnored { get; init; }
    public string Status { get; init; } = string.Empty;
}

public class SyncGmailEmailsCommandHandler : IRequestHandler<SyncGmailEmailsCommand, SyncResult>
{
    private readonly IGmailService _gmailService;
    private readonly IGmailConnectionRepository _connectionRepository;
    private readonly IEmailMessageRepository _emailRepository;
    private readonly IRuleRepository _ruleRepository;
    private readonly IRulesEngine _rulesEngine;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SyncGmailEmailsCommandHandler> _logger;

    public SyncGmailEmailsCommandHandler(
        IGmailService gmailService,
        IGmailConnectionRepository connectionRepository,
        IEmailMessageRepository emailRepository,
        IRuleRepository ruleRepository,
        IRulesEngine rulesEngine,
        IUnitOfWork unitOfWork,
        ILogger<SyncGmailEmailsCommandHandler> logger)
    {
        _gmailService = gmailService;
        _connectionRepository = connectionRepository;
        _emailRepository = emailRepository;
        _ruleRepository = ruleRepository;
        _rulesEngine = rulesEngine;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<SyncResult> Handle(SyncGmailEmailsCommand request, CancellationToken cancellationToken)
    {
        var connection = await _connectionRepository.GetActiveByUserIdAsync(request.UserId, cancellationToken);
        if (connection == null)
        {
            return new SyncResult { Status = "No active Gmail connection found." };
        }

        // Load user's active rules for filtering
        var rules = await _ruleRepository.GetActiveByUserIdAsync(request.UserId, cancellationToken);

        _logger.LogInformation(
            "Starting email sync for user {UserId} with {RuleCount} active rules",
            request.UserId, rules.Count);

        // Fetch emails with rule-based filtering (pre-filter at Gmail API level)
        var fetchedEmails = await _gmailService.FetchEmailsAsync(
            connection,
            rules.Any() ? rules : null, // Pass rules if any exist
            connection.LastSyncedAt, // Fetch since last sync
            null, // No historyId support yet in this basic implementation
            50, 
            cancellationToken);

        int savedCount = 0;
        int ignoredCount = 0;

        foreach (var email in fetchedEmails)
        {
            // Deduplicate by ContentHash
            if (await _emailRepository.ExistsByContentHashAsync(email.ContentHash, cancellationToken))
            {
                continue;
            }

            // Check if MessageId exists (double check)
            if (await _emailRepository.GetByMessageIdAsync(email.MessageId, cancellationToken) != null)
            {
                continue;
            }

            // Run through Rules Engine to determine what to do with this email
            var evaluation = await _rulesEngine.EvaluateAsync(email, rules, cancellationToken);
            
            // Set the directive on the email for auditing
            email.SetDirective(evaluation);

            _logger.LogInformation(
                "Email {MessageId} from {Sender}: {Directive} - {Reason}",
                email.MessageId, email.From.Value, evaluation.Directive, evaluation.Reason);

            // Only save emails that should be processed
            if (evaluation.ShouldProcess)
            {
                await _emailRepository.AddAsync(email, cancellationToken);
                savedCount++;
            }
            else
            {
                ignoredCount++;
                _logger.LogDebug("Email {MessageId} ignored: {Reason}", email.MessageId, evaluation.Reason);
            }
        }

        // Update connection last sync time
        if (fetchedEmails.Count > 0)
        {
            // We'd ideally get the latest historyId from the API response to store here
            // For now, just update the timestamp
            connection.UpdateLastSync(DateTime.UtcNow, connection.HistoryId ?? string.Empty);
            _connectionRepository.Update(connection);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Sync completed for user {UserId}: {Fetched} fetched, {Saved} saved, {Ignored} ignored",
            request.UserId, fetchedEmails.Count, savedCount, ignoredCount);

        return new SyncResult 
        { 
            EmailsFetched = fetchedEmails.Count, 
            EmailsSaved = savedCount,
            EmailsIgnored = ignoredCount,
            Status = "Success" 
        };
    }
}
