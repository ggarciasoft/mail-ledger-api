using MainLedger.Application.Authentication.Services;
using MainLedger.Application.Common.Interfaces;
using MainLedger.Application.Emails.Commands;
using MainLedger.Domain.Repositories;
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
    private readonly ILogger<GmailController> _logger;

    public GmailController(
        IGmailService gmailService,
        IMediator mediator,
        ICurrentUserService currentUserService,
        ILogger<GmailController> logger)
    {
        _gmailService = gmailService;
        _mediator = mediator;
        _currentUserService = currentUserService;
        _logger = logger;
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
    public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(state, out var userId))
        {
            return BadRequest("Invalid state parameter");
        }

        try
        {
            var connection = await _gmailService.HandleCallbackAsync(userId, code, cancellationToken);
            return Ok(new { message = "Gmail connected successfully", email = connection.Email.ToString() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Gmail callback for user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Triggers email synchronization for the current user's connected Gmail account.
    /// Emails are saved with Pending status for batch processing.
    /// </summary>
    [HttpPost("sync")]
    public async Task<IActionResult> SyncEmails(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            _logger.LogWarning("User ID not found in authentication context");
            return Unauthorized(new { error = "User not authenticated" });
        }

        try
        {
            var command = new SyncGmailEmailsCommand(userId.Value);
            var result = await _mediator.Send(command, cancellationToken);
            
            return Ok(result);
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
        CancellationToken cancellationToken = default)
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
        CancellationToken cancellationToken = default)
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
}
