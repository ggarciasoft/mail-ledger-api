using MainLedger.Application.Authentication.Services;
using MainLedger.Application.Processing.Commands;
using MainLedger.Application.Processing.Queries;
using MainLedger.Contracts.Processing;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MainLedger.API.Controllers;

/// <summary>
/// Controller for processing management operations.
/// </summary>
[ApiController]
[Route("api/processing")]
[Authorize]
public class ProcessingController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ProcessingController> _logger;

    public ProcessingController(
        IMediator mediator,
        ICurrentUserService currentUserService,
        ILogger<ProcessingController> logger)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Get processing status for a user.
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<ProcessingStatusDto>> GetStatus(
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
            var query = new GetProcessingStatusQuery(userId.Value);
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting processing status for user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Trigger batch classification for pending emails.
    /// </summary>
    [HttpPost("classify")]
    public async Task<ActionResult<TriggerJobResponseDto>> TriggerClassification(
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
            var command = new TriggerClassificationCommand(userId.Value, batchSize);
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering classification for user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Trigger batch extraction for classified emails.
    /// </summary>
    [HttpPost("extract")]
    public async Task<ActionResult<TriggerJobResponseDto>> TriggerExtraction(
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
            var command = new TriggerExtractionCommand(userId.Value, batchSize);
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering extraction for user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }
}
