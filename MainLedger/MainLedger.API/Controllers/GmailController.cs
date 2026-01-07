using MainLedger.Application.Common.Interfaces;
using MainLedger.Application.Emails.Commands;
using MainLedger.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MainLedger.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GmailController : ControllerBase
{
    private readonly IGmailService _gmailService;
    private readonly IMediator _mediator;

    public GmailController(IGmailService gmailService, IMediator mediator)
    {
        _gmailService = gmailService;
        _mediator = mediator;
    }

    [HttpGet("auth-url")]
    public IActionResult GetAuthUrl([FromQuery] Guid userId)
    {
        // In a real app, userId would verify strongly against the authenticated user claim
        var url = _gmailService.GetAuthorizationUrl(userId);
        return Ok(new { url });
    }

    [HttpGet("callback")]
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
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Triggers email synchronization for a user's connected Gmail account.
    /// Emails are saved with Pending status for batch processing.
    /// </summary>
    [HttpPost("sync")]
    public async Task<IActionResult> SyncEmails([FromQuery] Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            var command = new SyncGmailEmailsCommand(userId);
            var result = await _mediator.Send(command, cancellationToken);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Triggers batch classification of pending emails using AI.
    /// Processes up to batchSize emails in parallel.
    /// </summary>
    [HttpPost("batch-classify")]
    public async Task<IActionResult> BatchClassifyEmails(
        [FromQuery] Guid userId,
        [FromQuery] int batchSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new BatchClassifyEmailsCommand(userId, batchSize);
            var result = await _mediator.Send(command, cancellationToken);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Triggers batch extraction of financial data from classified emails.
    /// Processes up to batchSize emails in parallel with normalization.
    /// </summary>
    [HttpPost("batch-extract")]
    public async Task<IActionResult> BatchExtractFinancialData(
        [FromQuery] Guid userId,
        [FromQuery] int batchSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new BatchExtractFinancialDataCommand(userId, batchSize);
            var result = await _mediator.Send(command, cancellationToken);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
