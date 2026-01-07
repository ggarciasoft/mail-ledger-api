using MainLedger.Application.Emails.Queries;
using MainLedger.Contracts.Common;
using MainLedger.Contracts.Emails;
using MainLedger.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MainLedger.API.Controllers;

/// <summary>
/// Controller for email management operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EmailsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<EmailsController> _logger;

    public EmailsController(IMediator mediator, ILogger<EmailsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated list of emails with optional filtering.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<EmailListItemDto>>> GetEmails(
        [FromQuery] Guid userId,
        [FromQuery] EmailProcessingStatus? status = null,
        [FromQuery] bool? isFinancial = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "receivedAt",
        [FromQuery] string sortOrder = "desc",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetEmailsQuery(userId, status, isFinancial, page, pageSize, sortBy, sortOrder);
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting emails for user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get detailed information about a specific email.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<EmailDto>> GetEmailById(
        [FromRoute] Guid id,
        [FromQuery] Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetEmailByIdQuery(id, userId);
            var result = await _mediator.Send(query, cancellationToken);

            if (result == null)
            {
                return NotFound(new { error = "Email not found or access denied" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email {EmailId} for user {UserId}", id, userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get email statistics for a user.
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<EmailStatisticsDto>> GetStatistics(
        [FromQuery] Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetEmailStatisticsQuery(userId);
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email statistics for user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }
}
