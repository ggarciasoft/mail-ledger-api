using MainLedger.Application.Authentication.Commands;
using MainLedger.Application.Authentication.Queries;
using MainLedger.Application.Authentication.Services;
using MainLedger.Contracts.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MainLedger.API.Controllers;

/// <summary>
/// Controller for API key management.
/// </summary>
[ApiController]
[Route("api/api-keys")]
[Authorize]
public class ApiKeysController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ApiKeysController> _logger;

    public ApiKeysController(
        IMediator mediator,
        ICurrentUserService currentUserService,
        ILogger<ApiKeysController> logger)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new API key.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CreateApiKeyResponse>> CreateApiKey(
        [FromBody] CreateApiKeyRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            _logger.LogWarning("User ID not found in authentication context");
            return Unauthorized(new { error = "User not authenticated" });
        }

        try
        {
            var command = new CreateApiKeyCommand(
                userId.Value,
                request.Name,
                request.Scopes,
                request.ExpiresAt);

            var result = await _mediator.Send(command, cancellationToken);

            return Ok(new CreateApiKeyResponse(
                result.ApiKeyId,
                result.ApiKey,
                result.Name,
                result.Scopes,
                result.ExpiresAt,
                "API key created successfully. Save this key - it will not be shown again!"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid API key creation request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating API key for user {UserId}", userId);
            return StatusCode(500, new { error = "An error occurred while creating the API key" });
        }
    }

    /// <summary>
    /// List all API keys for the authenticated user.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<Application.Authentication.Queries.ApiKeyDto>>> ListApiKeys(
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            _logger.LogWarning("User ID not found in authentication context");
            return Unauthorized(new { error = "User not authenticated" });
        }

        try
        {
            var query = new ListApiKeysQuery(userId.Value);
            var result = await _mediator.Send(query, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing API keys for user {UserId}", userId);
            return StatusCode(500, new { error = "An error occurred while listing API keys" });
        }
    }

    /// <summary>
    /// Revoke an API key.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> RevokeApiKey(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            _logger.LogWarning("User ID not found in authentication context");
            return Unauthorized(new { error = "User not authenticated" });
        }

        try
        {
            var command = new RevokeApiKeyCommand(userId.Value, id);
            var result = await _mediator.Send(command, cancellationToken);

            if (result)
            {
                return Ok(new { message = "API key revoked successfully" });
            }

            return BadRequest(new { error = "Failed to revoke API key" });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "API key {ApiKeyId} not found for user {UserId}", id, userId);
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to API key {ApiKeyId} by user {UserId}", id, userId);
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking API key {ApiKeyId} for user {UserId}", id, userId);
            return StatusCode(500, new { error = "An error occurred while revoking the API key" });
        }
    }
}
