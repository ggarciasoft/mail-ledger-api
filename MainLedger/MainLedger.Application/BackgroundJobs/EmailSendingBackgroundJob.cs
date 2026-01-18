using MainLedger.Application.Common.Interfaces;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.BackgroundJobs;

public class EmailSendingBackgroundJob
{
    private readonly IEmailNotificationRepository _emailRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailSendingBackgroundJob> _logger;

    public EmailSendingBackgroundJob(
        IEmailNotificationRepository emailRepository,
        IEmailService emailService,
        ILogger<EmailSendingBackgroundJob> logger
    )
    {
        _emailRepository = emailRepository;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting email sending background job");

        // Process in batches
        var pendingEmails = await _emailRepository.GetPendingAsync(20, cancellationToken);

        foreach (var email in pendingEmails)
        {
            try
            {
                await _emailService.SendAsync(
                    email.Recipient,
                    email.Subject,
                    email.Body,
                    cancellationToken
                );
                email.MarkAsSent();
                _logger.LogInformation("Successfully processed email {EmailId}", email.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process email {EmailId}", email.Id);
                email.MarkAsFailed(ex.Message);
            }

            await _emailRepository.UpdateAsync(email, cancellationToken);
        }

        if (pendingEmails.Count > 0)
        {
            _logger.LogInformation("Completed batch of {Count} emails", pendingEmails.Count);
        }
    }
}
