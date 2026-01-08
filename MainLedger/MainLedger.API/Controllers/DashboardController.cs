using MainLedger.Application.Dashboard.Queries;
using MainLedger.Contracts.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MainLedger.API.Controllers;

/// <summary>
/// Controller for dashboard and analytics operations.
/// </summary>
[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IMediator mediator, ILogger<DashboardController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard overview with aggregated statistics.
    /// </summary>
    [HttpGet("overview")]
    public async Task<ActionResult<DashboardOverviewDto>> GetOverview(
        [FromQuery] Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetDashboardOverviewQuery(userId);
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard overview for user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get spending trends over time.
    /// </summary>
    [HttpGet("spending-trends")]
    public async Task<ActionResult<SpendingTrendsDto>> GetSpendingTrends(
        [FromQuery] Guid userId,
        [FromQuery] string period = "month",
        [FromQuery] string groupBy = "day",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetSpendingTrendsQuery(userId, period, groupBy);
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting spending trends for user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get top merchants by spending.
    /// </summary>
    [HttpGet("top-merchants")]
    public async Task<ActionResult<TopMerchantsDto>> GetTopMerchants(
        [FromQuery] Guid userId,
        [FromQuery] int limit = 10,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetTopMerchantsQuery(userId, limit, startDate, endDate);
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top merchants for user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }
}
