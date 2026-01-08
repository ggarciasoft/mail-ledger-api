using MainLedger.Application.Processing.Commands;
using MainLedger.Application.Processing.Queries;
using MainLedger.Contracts.Processing;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MainLedger.API.Controllers;

/// <summary>
/// Controller for processing management operations.
/// </summary>
[ApiController]
[Route("api/processing")]
public class ProcessingController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProcessingController> _logger;

    public ProcessingController(IMediator mediator, ILogger<ProcessingController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get processing status for a user.
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<ProcessingStatusDto>> GetStatus(
        [FromQuery] Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetProcessingStatusQuery(userId);
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
        [FromQuery] Guid userId,
        [FromQuery] int batchSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new TriggerClassificationCommand(userId, batchSize);
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
        [FromQuery] Guid userId,
        [FromQuery] int batchSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new TriggerExtractionCommand(userId, batchSize);
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
