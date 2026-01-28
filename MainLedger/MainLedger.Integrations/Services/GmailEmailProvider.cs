using Hangfire;
using MainLedger.Application.BackgroundJobs;
using MainLedger.Application.Common.Interfaces;
using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using MainLedger.Domain.Services;

namespace MainLedger.Integrations.Services;

/// <summary>
/// Gmail email provider adapter that implements IEmailProvider interface.
/// Wraps the existing GmailService to work with the new multi-provider architecture.
/// </summary>
public class GmailEmailProvider : IEmailProvider
{
    private readonly IGmailService _gmailService;
    private readonly IEmailConnectionRepository _emailConnectionRepository;
    private readonly IProcessingJobRepository _jobRepository;
    private readonly IUnitOfWork _unitOfWork;

    public EmailProvider ProviderType => EmailProvider.Gmail;

    public GmailEmailProvider(
        IGmailService gmailService,
        IEmailConnectionRepository emailConnectionRepository,
        IProcessingJobRepository jobRepository,
        IUnitOfWork unitOfWork
    )
    {
        _gmailService = gmailService;
        _emailConnectionRepository = emailConnectionRepository;
        _jobRepository = jobRepository;
        _unitOfWork = unitOfWork;
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
            // Check if Gmail connection exists
            var emailConnection = await _emailConnectionRepository.GetByUserAndProviderAsync(
                userId,
                EmailProvider.Gmail
            );

            if (emailConnection == null || !emailConnection.IsActive)
            {
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
                $"MaxEmails: {options.MaxResults ?? 50}"
            );

            await _jobRepository.AddAsync(job, CancellationToken.None);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);

            // Enqueue background job using Hangfire
            var hangfireJobId = BackgroundJob.Enqueue<EmailSyncBackgroundJob>(x =>
                x.ExecuteAsync(job.Id, userId, options.MaxResults ?? 50, default)
            );

            job.SetHangfireJobId(hangfireJobId);
            _jobRepository.Update(job);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);

            // Return immediately - the background job will handle the actual sync
            // Note: Frontend should poll for job status or use SignalR for real-time updates
            return new SyncResult
            {
                EmailsSynced = 0,
                EmailsSkipped = 0,
                Errors = new List<string>(), // Empty errors - job queued successfully
            };
        }
        catch (Exception ex)
        {
            return new SyncResult
            {
                EmailsSynced = 0,
                EmailsSkipped = 0,
                Errors = new List<string> { $"Failed to queue sync job: {ex.Message}" },
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
