using MainLedger.Application.Authentication.Services;
using MainLedger.Application.FinancialRecords.Queries;
using MainLedger.Contracts.Common;
using MainLedger.Contracts.FinancialRecords;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MainLedger.API.Controllers;

/// <summary>
/// Controller for financial records (confirmed transactions) operations.
/// Read-only API for viewing confirmed financial data.
/// </summary>
[ApiController]
[Route("api/financial-records")]
[Authorize]
public class FinancialRecordsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<FinancialRecordsController> _logger;

    public FinancialRecordsController(
        IMediator mediator,
        ICurrentUserService currentUserService,
        ILogger<FinancialRecordsController> logger
    )
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated list of confirmed financial records with extensive filtering.
    /// </summary>
    [HttpGet]
    [MainLedger.Infrastructure.Security.RequireScope("read:transactions")]
    public async Task<ActionResult<PaginatedResponse<FinancialRecordListItemDto>>> GetRecords(
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
            // Convert dates to UTC for PostgreSQL compatibility
            var startDateUtc = startDate.HasValue
                ? DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc)
                : (DateTime?)null;
            var endDateUtc = endDate.HasValue
                ? DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc)
                : (DateTime?)null;

            var query = new GetFinancialRecordsQuery(
                userId.Value,
                startDateUtc,
                endDateUtc,
                minAmount,
                maxAmount,
                merchant,
                currency,
                sourceBank,
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
            _logger.LogError(ex, "Error getting financial records for user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get detailed information about a specific financial record.
    /// </summary>
    [HttpGet("{id}")]
    [MainLedger.Infrastructure.Security.RequireScope("read:transactions")]
    public async Task<ActionResult<FinancialRecordDto>> GetById(
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
            var query = new GetFinancialRecordByIdQuery(id, userId.Value);
            var result = await _mediator.Send(query, cancellationToken);

            if (result == null)
            {
                return NotFound(new { error = "Financial record not found or access denied" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting financial record {RecordId} for user {UserId}",
                id,
                userId
            );
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get financial record statistics and trends for a user.
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<FinancialRecordStatisticsDto>> GetStatistics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
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
            var query = new GetFinancialRecordStatisticsQuery(userId.Value, startDate, endDate);
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting financial record statistics for user {UserId}",
                userId
            );
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Export financial records to CSV or JSON.
    /// </summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string format = "csv",
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] decimal? minAmount = null,
        [FromQuery] decimal? maxAmount = null,
        [FromQuery] string? merchant = null,
        [FromQuery] string? currency = null,
        [FromQuery] string? sourceBank = null,
        [FromQuery] string sortBy = "transactionDate",
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
            // Parse format
            var exportFormat = format.ToLower() == "json" ? ExportFormat.Json : ExportFormat.Csv;

            // Convert dates to UTC
            var startDateUtc = startDate.HasValue
                ? DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc)
                : (DateTime?)null;
            var endDateUtc = endDate.HasValue
                ? DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc)
                : (DateTime?)null;

            var query = new ExportFinancialRecordsQuery(
                userId.Value,
                exportFormat,
                startDateUtc,
                endDateUtc,
                minAmount,
                maxAmount,
                merchant,
                currency,
                sourceBank,
                sortBy,
                sortOrder
            );

            var result = await _mediator.Send(query, cancellationToken);

            return File(result.Content, result.ContentType, result.FileName);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "User {UserId} unauthorized to export records", userId);
            return StatusCode(403, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting financial records for user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }
}
