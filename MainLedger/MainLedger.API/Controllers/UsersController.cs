using MainLedger.Application.Authentication.Queries;
using MainLedger.Contracts.Authentication;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MainLedger.API.Controllers;

/// <summary>
/// Controller for user profile operations.
/// </summary>
[ApiController]
[Route("api/users")]
// [Authorize] // TODO: Add authorization
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IMediator mediator, ILogger<UsersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get current user's profile.
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<Application.Authentication.Queries.UserDto>> GetCurrentUser(
        CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: Get userId from authenticated user
            var userId = Guid.Empty; // Placeholder

            var query = new GetUserByIdQuery(userId);
            var result = await _mediator.Send(query, cancellationToken);

            if (result == null)
            {
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
