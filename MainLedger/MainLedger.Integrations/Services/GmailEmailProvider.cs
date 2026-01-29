using Hangfire;
using MainLedger.Application.BackgroundJobs;
using MainLedger.Application.Common.Interfaces;
using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using MainLedger.Domain.Services;
using Microsoft.Extensions.Logging;

namespace MainLedger.Integrations.Services;

/// <summary>
/// Gmail email provider adapter that implements IEmailProvider interface.
/// Wraps the existing GmailService to work with the new multi-provider architecture.
/// </summary>
public class GmailEmailProvider : IEmailProvider
{
    private readonly IGmailService _gmailService;
    private readonly IEmailConnectionRepository _emailConnectionRepository;
    private readonly IEmailMessageRepository _emailMessageRepository;
    private readonly IRuleRepository _ruleRepository;
    private readonly IProcessingJobRepository _jobRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GmailEmailProvider> _logger;

    public EmailProvider ProviderType => EmailProvider.Gmail;

    public GmailEmailProvider(
        IGmailService gmailService,
        IEmailConnectionRepository emailConnectionRepository,
        IEmailMessageRepository emailMessageRepository,
        IRuleRepository ruleRepository,
        IProcessingJobRepository jobRepository,
        IUnitOfWork unitOfWork,
        ILogger<GmailEmailProvider> logger
    )
    {
        _gmailService = gmailService;
        _emailConnectionRepository = emailConnectionRepository;
        _emailMessageRepository = emailMessageRepository;
        _ruleRepository = ruleRepository;
        _jobRepository = jobRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public Task<OAuthUrlResult> GetAuthorizationUrlAsync(Guid userId)
    {
        var authUrl = _gmailService.GetAuthorizationUrl(userId);
        var result = new OAuthUrlResult
        {
            AuthorizationUrl = authUrl,
            State = string.Empty, // Gmail service doesn't return state separately
        };
        return Task.FromResult(result);
    }

    public async Task<ConnectionResult> HandleOAuthCallbackAsync(string code, string state, Guid userId)
    {
        try
        {
            // GmailService now saves to EmailConnection table only
            var emailConnection = await _gmailService.HandleCallbackAsync(
                userId,
                code,
                CancellationToken.None
            );

            return new ConnectionResult { Success = true, Email = emailConnection.Email };
        }
        catch (Exception ex)
        {
            return new ConnectionResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<SyncResult> SyncEmailsAsync(Guid userId, SyncOptions options)
    {
        try
        {
            _logger.LogInformation("Enqueueing Gmail sync job for user {UserId}", userId);

            // Check if Gmail connection exists
            var emailConnection = await _emailConnectionRepository.GetByUserAndProviderAsync(
                userId,
                EmailProvider.Gmail
            );

            if (emailConnection == null || !emailConnection.IsActive)
            {
                _logger.LogWarning("No active Gmail connection found for user {UserId}", userId);
                return new SyncResult
                {
                    EmailsSynced = 0,
                    EmailsSkipped = 0,
                    Errors = new List<string>
                    {
                        "No active Gmail connection found. Please connect your Gmail account first.",
                    },
                };
            }

            // Create a processing job for tracking
            var job = ProcessingJob.Create(
                userId,
                JobType.EmailSync,
                string.Empty, // Hangfire job ID will be set after enqueueing
                $"Provider: Gmail, MaxEmails: {options.MaxResults ?? 100}"
            );

            await _jobRepository.AddAsync(job, CancellationToken.None);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);

            // Enqueue background job using Hangfire
            var hangfireJobId = BackgroundJob.Enqueue<EmailSyncBackgroundJob>(x =>
                x.ExecuteAsync(job.Id, userId, EmailProvider.Gmail, options.MaxResults ?? 100, default)
            );

            job.SetHangfireJobId(hangfireJobId);
            _jobRepository.Update(job);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);

            _logger.LogInformation(
                "Gmail sync job {JobId} enqueued with Hangfire ID {HangfireJobId}",
                job.Id,
                hangfireJobId
            );

            // Return immediately - the background job will handle the actual sync
            // Frontend should poll for job status or use SignalR for real-time updates
            return new SyncResult
            {
                EmailsSynced = 0,
                EmailsSkipped = 0,
                Errors = new List<string>(), // Empty errors - job queued successfully
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue Gmail sync job for user {UserId}", userId);
            return new SyncResult
            {
                EmailsSynced = 0,
                EmailsSkipped = 0,
                Errors = new List<string> { $"Failed to queue sync job: {ex.Message}" },
            };
        }
    }

    /// <summary>
    /// Internal method to perform the actual Gmail email sync.
    /// Called by EmailSyncBackgroundJob.
    /// </summary>
    public async Task<SyncResult> PerformSyncAsync(Guid userId, SyncOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting Gmail sync for user {UserId}", userId);

            // Check if Gmail connection exists
            var emailConnection = await _emailConnectionRepository.GetByUserAndProviderAsync(
                userId,
                EmailProvider.Gmail
            );

            if (emailConnection == null || !emailConnection.IsActive)
            {
                _logger.LogWarning("No active Gmail connection found for user {UserId}", userId);
                return new SyncResult
                {
                    EmailsSynced = 0,
                    EmailsSkipped = 0,
                    Errors = new List<string>
                    {
                        "No active Gmail connection found. Please connect your Gmail account first.",
                    },
                };
            }

            var result = new SyncResult { Errors = new List<string>() };

            // Load user's active rules
            var rules = await _ruleRepository.GetActiveByUserIdAsync(userId, cancellationToken);

            // Fetch emails from Gmail
            var fetchedEmails = await _gmailService.FetchEmailsAsync(
                emailConnection,
                rules.Any() ? rules.ToList() : null,
                options.SyncFrom,
                null,
                options.MaxResults ?? 100,
                cancellationToken
            );

            _logger.LogInformation(
                "Fetched {Count} emails from Gmail for user {UserId}",
                fetchedEmails.Count,
                userId
            );

            // Process each email
            foreach (var email in fetchedEmails)
            {
                try
                {
                    // Check if email already exists by content hash
                    var exists = await _emailMessageRepository.ExistsByContentHashAsync(
                        email.ContentHash,
                        cancellationToken
                    );

                    if (exists)
                    {
                        result.EmailsSkipped++;
                        continue;
                    }

                    // Save email
                    await _emailMessageRepository.AddAsync(email, cancellationToken);
                    result.EmailsSynced++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save Gmail email {MessageId}", email.MessageId);
                    result.Errors.Add($"Failed to save email {email.MessageId}: {ex.Message}");
                    result.EmailsSkipped++;
                }
            }

            // Update connection last synced time
            emailConnection.LastSyncedAt = DateTime.UtcNow;
            await _emailConnectionRepository.UpdateAsync(emailConnection);

            // Save all changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Gmail sync completed for user {UserId}. Synced: {Synced}, Skipped: {Skipped}",
                userId,
                result.EmailsSynced,
                result.EmailsSkipped
            );

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gmail sync failed for user {UserId}", userId);
            return new SyncResult
            {
                EmailsSynced = 0,
                EmailsSkipped = 0,
                Errors = new List<string> { $"Sync failed: {ex.Message}" },
            };
        }
    }

    public async Task<ConnectionStatus> GetConnectionStatusAsync(Guid userId)
    {
        var connection = await _emailConnectionRepository.GetByUserAndProviderAsync(
            userId,
            EmailProvider.Gmail
        );

        if (connection == null)
        {
            return new ConnectionStatus
            {
                IsConnected = false,
                Email = string.Empty,
                LastSyncedAt = null,
            };
        }

        return new ConnectionStatus
        {
            IsConnected = connection.IsActive,
            Email = connection.Email,
            LastSyncedAt = connection.LastSyncedAt,
        };
    }

    public async Task DisconnectAsync(Guid userId)
    {
        // Disconnect from the unified EmailConnection table
        var emailConnection = await _emailConnectionRepository.GetByUserAndProviderAsync(
            userId,
            EmailProvider.Gmail
        );

        if (emailConnection != null)
        {
            emailConnection.IsActive = false;
            await _emailConnectionRepository.UpdateAsync(emailConnection);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);
        }
    }
}
