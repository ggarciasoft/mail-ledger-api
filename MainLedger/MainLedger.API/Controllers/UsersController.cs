using MainLedger.Application.Authentication.Queries;
using MainLedger.Application.Authentication.Services;
using MainLedger.Contracts.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MainLedger.API.Controllers;

/// <summary>
/// Controller for user profile operations.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize] // Require authentication for all endpoints
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IMediator mediator,
        ICurrentUserService currentUserService,
        ILogger<UsersController> logger)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Get current authenticated user's profile.
    /// </summary>
    /// <response code="200">Returns the user profile</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="404">User not found</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(Application.Authentication.Queries.UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Application.Authentication.Queries.UserDto>> GetCurrentUser(
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get userId from authenticated user context
            var userId = _currentUserService.GetUserId();
            
            if (userId == null)
            {
                _logger.LogWarning("User ID not found in authentication context");
                return Unauthorized(new { error = "User not authenticated" });
            }

            var query = new GetUserByIdQuery(userId.Value);
            var result = await _mediator.Send(query, cancellationToken);

            if (result == null)
            {
                _logger.LogWarning("User {UserId} not found in database", userId);
                return NotFound(new { error = "User not found" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(500, new { error = "An error occurred while retrieving user information" });
        }
    }
}
