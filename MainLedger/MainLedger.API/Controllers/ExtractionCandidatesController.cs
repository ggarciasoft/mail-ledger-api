using MainLedger.Application.ExtractionCandidates.Commands;
using MainLedger.Application.ExtractionCandidates.Queries;
using MainLedger.Contracts.Common;
using MainLedger.Contracts.ExtractionCandidates;
using MainLedger.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MainLedger.API.Controllers;

/// <summary>
/// Controller for extraction candidate management operations.
/// </summary>
[ApiController]
[Route("api/extraction-candidates")]
public class ExtractionCandidatesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ExtractionCandidatesController> _logger;

    public ExtractionCandidatesController(IMediator mediator, ILogger<ExtractionCandidatesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated list of extraction candidates with optional filtering.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ExtractionCandidateListItemDto>>> GetCandidates(
        [FromQuery] Guid userId,
        [FromQuery] RecordStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "createdAt",
        [FromQuery] string sortOrder = "desc",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetExtractionCandidatesQuery(userId, status, page, pageSize, sortBy, sortOrder);
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting extraction candidates for user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get detailed information about a specific extraction candidate.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ExtractionCandidateDto>> GetById(
        [FromRoute] Guid id,
        [FromQuery] Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetExtractionCandidateByIdQuery(id, userId);
            var result = await _mediator.Send(query, cancellationToken);

            if (result == null)
            {
                return NotFound(new { error = "Extraction candidate not found or access denied" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting extraction candidate {CandidateId} for user {UserId}", id, userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Confirm an extraction candidate and create a financial record.
    /// </summary>
    [HttpPost("{id}/confirm")]
    public async Task<IActionResult> Confirm(
        [FromRoute] Guid id,
        [FromQuery] Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new ConfirmExtractionCandidateCommand(id, userId);
            var financialRecordId = await _mediator.Send(command, cancellationToken);

            return Ok(new
            {
                success = true,
                message = "Extraction confirmed",
                financialRecordId
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming extraction candidate {CandidateId} for user {UserId}", id, userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Reject an extraction candidate.
    /// </summary>
    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(
        [FromRoute] Guid id,
        [FromQuery] Guid userId,
        [FromBody] RejectExtractionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new RejectExtractionCandidateCommand(id, userId, request.Reason);
            await _mediator.Send(command, cancellationToken);

            return Ok(new { success = true, message = "Extraction rejected" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting extraction candidate {CandidateId} for user {UserId}", id, userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update an extraction candidate before confirmation.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromQuery] Guid userId,
        [FromBody] UpdateExtractionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new UpdateExtractionCandidateCommand(
                id, userId,
                request.Amount, request.Currency, request.Merchant,
                request.TransactionDate, request.SourceAccount, request.TargetAccount,
                request.SourceBank, request.TargetBank, request.Fees, request.Tax, request.ReferenceId);

            await _mediator.Send(command, cancellationToken);

            return Ok(new { success = true, message = "Extraction candidate updated" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating extraction candidate {CandidateId} for user {UserId}", id, userId);
            return BadRequest(new { error = ex.Message });
        }
    }
}
