using MainLedger.Application.Authentication.Services;
using MainLedger.Application.Dashboard.Queries;
using MainLedger.Contracts.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MainLedger.API.Controllers;

/// <summary>
/// Controller for dashboard and analytics operations.
/// </summary>
[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IMediator mediator,
        ICurrentUserService currentUserService,
        ILogger<DashboardController> logger
    )
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard overview with aggregated statistics.
    /// </summary>
    [HttpGet("overview")]
    [MainLedger.Infrastructure.Security.RequireScope("read:transactions")]
    public async Task<ActionResult<DashboardOverviewDto>> GetOverview(
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
            var query = new GetDashboardOverviewQuery(userId.Value);
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
    [MainLedger.Infrastructure.Security.RequireScope("read:transactions")]
    public async Task<ActionResult<SpendingTrendsDto>> GetSpendingTrends(
        [FromQuery] string period = "month",
        [FromQuery] string groupBy = "day",
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
            var query = new GetSpendingTrendsQuery(userId.Value, period, groupBy);
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
    [MainLedger.Infrastructure.Security.RequireScope("read:transactions")]
    public async Task<ActionResult<TopMerchantsDto>> GetTopMerchants(
        [FromQuery] int limit = 10,
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
            var query = new GetTopMerchantsQuery(userId.Value, limit, startDate, endDate);
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
