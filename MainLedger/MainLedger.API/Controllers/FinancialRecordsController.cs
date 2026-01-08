using MainLedger.Application.FinancialRecords.Queries;
using MainLedger.Contracts.Common;
using MainLedger.Contracts.FinancialRecords;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MainLedger.API.Controllers;

/// <summary>
/// Controller for financial records (confirmed transactions) operations.
/// Read-only API for viewing confirmed financial data.
/// </summary>
[ApiController]
[Route("api/financial-records")]
public class FinancialRecordsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<FinancialRecordsController> _logger;

    public FinancialRecordsController(IMediator mediator, ILogger<FinancialRecordsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated list of confirmed financial records with extensive filtering.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<FinancialRecordListItemDto>>> GetRecords(
        [FromQuery] Guid userId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] decimal? minAmount = null,
        [FromQuery] decimal? maxAmount = null,
        [FromQuery] string? merchant = null,
        [FromQuery] string? currency = null,
        [FromQuery] string? sourceBank = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "transactionDate",
        [FromQuery] string sortOrder = "desc",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetFinancialRecordsQuery(
                userId, startDate, endDate, minAmount, maxAmount,
                merchant, currency, sourceBank, page, pageSize, sortBy, sortOrder);
            
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting financial records for user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get detailed information about a specific financial record.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<FinancialRecordDto>> GetById(
        [FromRoute] Guid id,
        [FromQuery] Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetFinancialRecordByIdQuery(id, userId);
            var result = await _mediator.Send(query, cancellationToken);

            if (result == null)
            {
                return NotFound(new { error = "Financial record not found or access denied" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting financial record {RecordId} for user {UserId}", id, userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get financial record statistics and trends for a user.
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<FinancialRecordStatisticsDto>> GetStatistics(
        [FromQuery] Guid userId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetFinancialRecordStatisticsQuery(userId, startDate, endDate);
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting financial record statistics for user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }
}
