using MainLedger.Application.Authentication.Services;
using MainLedger.Application.Common.Interfaces;
using MainLedger.Application.ExtractionCandidates.Commands;
using MainLedger.Application.ExtractionCandidates.Queries;
using MainLedger.Contracts.Common;
using MainLedger.Contracts.ExtractionCandidates;
using MainLedger.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MainLedger.API.Controllers;

/// <summary>
/// Controller for extraction candidate management operations.
/// </summary>
[ApiController]
[Route("api/extraction-candidates")]
[Authorize]
public class ExtractionCandidatesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<ExtractionCandidatesController> _logger;

    public ExtractionCandidatesController(
        IMediator mediator,
        ICurrentUserService currentUserService,
        ISubscriptionService subscriptionService,
        ILogger<ExtractionCandidatesController> logger
    )
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated list of extraction candidates with optional filtering.
    /// </summary>
    [HttpGet]
    public async Task<
        ActionResult<PaginatedResponse<ExtractionCandidateListItemDto>>
    > GetCandidates(
        [FromQuery] RecordStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "createdAt",
        [FromQuery] string sortOrder = "desc",
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
            var query = new GetExtractionCandidatesQuery(
                userId.Value,
                status,
                page,
                pageSize,
                sortBy,
                sortOrder
            );
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
    [MainLedger.Infrastructure.Security.RequireScope("read:transactions")]
    public async Task<ActionResult<ExtractionCandidateDto>> GetById(
        [FromRoute] Guid id,
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
            var query = new GetExtractionCandidateByIdQuery(id, userId.Value);
            var result = await _mediator.Send(query, cancellationToken);

            if (result == null)
            {
                return NotFound(new { error = "Extraction candidate not found or access denied" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting extraction candidate {CandidateId} for user {UserId}",
                id,
                userId
            );
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Confirm an extraction candidate and create a financial record.
    /// </summary>
    [HttpPost("{id}/confirm")]
    public async Task<IActionResult> Confirm(
        [FromRoute] Guid id,
        [FromBody] ConfirmCandidateRequest? request,
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
            var command = new ConfirmExtractionCandidateCommand(
                id,
                userId.Value,
                request?.Merchant
            );
            var financialRecordId = await _mediator.Send(command, cancellationToken);

            return Ok(
                new
                {
                    success = true,
                    message = "Extraction confirmed",
                    financialRecordId,
                }
            );
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
            _logger.LogError(
                ex,
                "Error confirming extraction candidate {CandidateId} for user {UserId}",
                id,
                userId
            );
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Reject an extraction candidate.
    /// </summary>
    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(
        [FromRoute] Guid id,
        [FromBody] RejectExtractionRequest request,
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
            var command = new RejectExtractionCandidateCommand(id, userId.Value, request.Reason);
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
            _logger.LogError(
                ex,
                "Error rejecting extraction candidate {CandidateId} for user {UserId}",
                id,
                userId
            );
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update an extraction candidate before confirmation.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateExtractionRequest request,
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
            var command = new UpdateExtractionCandidateCommand(
                id,
                userId.Value,
                request.Amount,
                request.Currency,
                request.Merchant,
                request.TransactionDate,
                request.SourceAccount,
                request.TargetAccount,
                request.SourceBank,
                request.TargetBank,
                request.Fees,
                request.Tax,
                request.ReferenceId
            );

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
            _logger.LogError(
                ex,
                "Error updating extraction candidate {CandidateId} for user {UserId}",
                id,
                userId
            );
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Bulk confirm multiple extraction candidates.
    /// </summary>
    [HttpPost("bulk-confirm")]
    public async Task<ActionResult<BulkOperationResponse>> BulkConfirm(
        [FromBody] BulkConfirmRequest request,
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
            // Check subscription permissions for bulk operations
            var canUseBulkOps = await _subscriptionService.CanUseBulkOperationsAsync(
                userId.Value,
                cancellationToken
            );
            if (!canUseBulkOps)
            {
                return Forbid(
                    "Bulk operations are not available on your subscription plan. Please upgrade to use bulk confirm/reject."
                );
            }

            var command = new BulkConfirmExtractionCandidatesCommand(
                request.CandidateIds,
                userId.Value
            );

            var result = await _mediator.Send(command, cancellationToken);

            if (result.Succeeded == 0)
            {
                return BadRequest(new { error = "All confirmations failed", details = result });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error bulk confirming extraction candidates for user {UserId}",
                userId
            );
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Bulk reject multiple extraction candidates.
    /// </summary>
    [HttpPost("bulk-reject")]
    public async Task<ActionResult<BulkOperationResponse>> BulkReject(
        [FromBody] BulkRejectRequest request,
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
            // Check subscription permissions for bulk operations
            var canUseBulkOps = await _subscriptionService.CanUseBulkOperationsAsync(
                userId.Value,
                cancellationToken
            );
            if (!canUseBulkOps)
            {
                return Forbid(
                    "Bulk operations are not available on your subscription plan. Please upgrade to use bulk confirm/reject."
                );
            }

            var command = new BulkRejectExtractionCandidatesCommand(
                request.CandidateIds,
                userId.Value,
                request.Reason
            );

            var result = await _mediator.Send(command, cancellationToken);

            if (result.Succeeded == 0)
            {
                return BadRequest(new { error = "All rejections failed", details = result });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error bulk rejecting extraction candidates for user {UserId}",
                userId
            );
            return BadRequest(new { error = ex.Message });
        }
    }
}
