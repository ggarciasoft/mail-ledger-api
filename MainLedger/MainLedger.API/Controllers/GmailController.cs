using MainLedger.Application.Authentication.Services;
using MainLedger.Application.Common.Interfaces;
using MainLedger.Application.Emails.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MainLedger.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Require authentication for all endpoints
public class GmailController : ControllerBase
{
    private readonly IGmailService _gmailService;
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<GmailController> _logger;
    private readonly MainLedger.Application.Common.Interfaces.IJobManagementService _jobManagementService;

    public GmailController(
        IGmailService gmailService,
        IMediator mediator,
        ICurrentUserService currentUserService,
        ISubscriptionService subscriptionService,
        ILogger<GmailController> logger,
        MainLedger.Application.Common.Interfaces.IJobManagementService jobManagementService
    )
    {
        _gmailService = gmailService;
        _mediator = mediator;
        _currentUserService = currentUserService;
        _subscriptionService = subscriptionService;
        _logger = logger;
        _jobManagementService = jobManagementService;
    }

    /// <summary>
    /// Gets the Gmail OAuth authorization URL for the current authenticated user.
    /// </summary>
    [HttpGet("auth-url")]
    public IActionResult GetAuthUrl()
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            _logger.LogWarning("User ID not found in authentication context");
            return Unauthorized(new { error = "User not authenticated" });
        }

        var url = _gmailService.GetAuthorizationUrl(userId.Value);
        return Ok(new { url });
    }

    /// <summary>
    /// Handles the OAuth callback from Gmail.
    /// </summary>
    [HttpGet("callback")]
    [AllowAnonymous] // Allow anonymous for OAuth callback
    public async Task<IActionResult> Callback(
        [FromQuery] string code,
        [FromQuery] string state,
        CancellationToken cancellationToken
    )
    {
        if (!Guid.TryParse(state, out var userId))
        {
            return BadRequest("Invalid state parameter");
        }

        try
        {
            // Check subscription limits
            var canConnect = await _subscriptionService.CanConnectGmailAccountAsync(
                userId,
                cancellationToken
            );
            if (!canConnect)
            {
                // Redirect to settings with error
                return Redirect("http://localhost:5173/settings?error=gmail_limit_reached");
            }

            var connection = await _gmailService.HandleCallbackAsync(
                userId,
                code,
                cancellationToken
            );

            // Redirect to settings page with success
            return Redirect("http://localhost:5173/settings?gmail_connected=true");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Gmail callback for user {UserId}", userId);
            // Redirect to settings with error
            return Redirect($"http://localhost:5173/settings?error=gmail_connection_failed");
        }
    }

    /// <summary>
    /// Triggers email synchronization for the current user's connected Gmail account.
    /// Emails are saved with Pending status for batch processing.
    /// </summary>
    /// <param name="maxEmails">Maximum number of emails to fetch (default: 50, max: 100)</param>
    [HttpPost("sync")]
    public async Task<IActionResult> SyncEmails(
        [FromQuery] int maxEmails = 50,
        CancellationToken cancellationToken = default
    )
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            _logger.LogWarning("User ID not found in authentication context");
            return Unauthorized(new { error = "User not authenticated" });
        }

        // Validate maxEmails parameter
        if (maxEmails < 1 || maxEmails > 100)
        {
            return BadRequest(new { error = "maxEmails must be between 1 and 100" });
        }

        try
        {
            // Create job using service (handles validation and race condition protection)
            var job = await _jobManagementService.CreateJobAsync(
                userId.Value,
                Domain.Enums.JobType.EmailSync,
                System.Text.Json.JsonSerializer.Serialize(new { maxEmails }),
                cancellationToken
            );

            if (job == null)
            {
                return Conflict(
                    new
                    {
                        error = "An email sync job is already running. Please wait for it to complete.",
                    }
                );
            }

            // Enqueue Hangfire background job
            var hangfireJobId =
                Hangfire.BackgroundJob.Enqueue<Application.BackgroundJobs.EmailSyncBackgroundJob>(
                    x => x.ExecuteAsync(job.Id, userId.Value, maxEmails, CancellationToken.None)
                );

            // Update job with Hangfire job ID
            job.SetHangfireJobId(hangfireJobId);
            await _jobManagementService.UpdateJobAsync(job, cancellationToken);

            _logger.LogInformation(
                "Enqueued email sync job {JobId} (Hangfire: {HangfireJobId}) for user {UserId}",
                job.Id,
                hangfireJobId,
                userId
            );

            return Ok(new { jobId = job.Id, message = "Email sync job enqueued successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing emails for user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Triggers batch classification of pending emails using AI.
    /// Processes up to batchSize emails in parallel.
    /// </summary>
    [HttpPost("batch-classify")]
    public async Task<IActionResult> BatchClassifyEmails(
        [FromQuery] int batchSize = 20,
        CancellationToken cancellationToken = default
    )
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            _logger.LogWarning("User ID not found in authentication context");
            return Unauthorized(new { error = "User not authenticated" });
        }

        try
        {
            var command = new BatchClassifyEmailsCommand(userId.Value, batchSize);
            var result = await _mediator.Send(command, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error batch classifying emails for user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Triggers batch extraction of financial data from classified emails.
    /// Processes up to batchSize emails in parallel with normalization.
    /// </summary>
    [HttpPost("batch-extract")]
    public async Task<IActionResult> BatchExtractFinancialData(
        [FromQuery] int batchSize = 20,
        CancellationToken cancellationToken = default
    )
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            _logger.LogWarning("User ID not found in authentication context");
            return Unauthorized(new { error = "User not authenticated" });
        }

        try
        {
            var command = new BatchExtractFinancialDataCommand(userId.Value, batchSize);
            var result = await _mediator.Send(command, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error batch extracting financial data for user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get Gmail connection status for the current user.
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetConnectionStatus(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            _logger.LogWarning("User ID not found in authentication context");
            return Unauthorized(new { error = "User not authenticated" });
        }

        try
        {
            var query = new MainLedger.Application.Gmail.Queries.GetGmailConnectionStatusQuery(
                userId.Value
            );
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Gmail connection status for user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get sync history for the current user.
    /// </summary>
    [HttpGet("sync-history")]
    public async Task<IActionResult> GetSyncHistory(
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default
    )
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            _logger.LogWarning("User ID not found in authentication context");
            return Unauthorized(new { error = "User not authenticated" });
        }

        try
        {
            var query = new MainLedger.Application.Gmail.Queries.GetSyncHistoryQuery(
                userId.Value,
                limit
            );
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sync history for user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }
}
