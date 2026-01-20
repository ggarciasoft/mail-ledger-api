using MainLedger.Application.Authentication.Services;
using MainLedger.Application.Categories.Queries;
using MainLedger.Contracts.Categories;
using MainLedger.Infrastructure.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MainLedger.API.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(
        IMediator mediator,
        ICurrentUserService currentUserService,
        ILogger<CategoriesController> logger
    )
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Get all categories for the current user.
    /// </summary>
    [HttpGet]
    [RequireScope("read:transactions")]
    public async Task<ActionResult<CategoryListResponse>> GetCategories(
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
            var query = new GetCategoriesQuery(userId.Value);
            var result = await _mediator.Send(query, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories for user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }
}
