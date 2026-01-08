using MainLedger.Application.Authentication.Commands;
using MainLedger.Application.Authentication.Queries;
using MainLedger.Contracts.Authentication;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MainLedger.API.Controllers;

/// <summary>
/// Controller for API key management.
/// </summary>
[ApiController]
[Route("api/api-keys")]
// [Authorize] // TODO: Add authorization
public class ApiKeysController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ApiKeysController> _logger;

    public ApiKeysController(IMediator mediator, ILogger<ApiKeysController> logger)
    {
        _mediator = mediator;
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
        try
        {
            // TODO: Get userId from authenticated user
            var userId = Guid.Empty; // Placeholder

            var command = new CreateApiKeyCommand(
                userId,
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
            _logger.LogError(ex, "Error creating API key");
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
        try
        {
            // TODO: Get userId from authenticated user
            var userId = Guid.Empty; // Placeholder

            var query = new ListApiKeysQuery(userId);
            var result = await _mediator.Send(query, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing API keys");
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
        try
        {
            // TODO: Get userId from authenticated user
            var userId = Guid.Empty; // Placeholder

            var command = new RevokeApiKeyCommand(userId, id);
            var result = await _mediator.Send(command, cancellationToken);

            if (result)
            {
                return Ok(new { message = "API key revoked successfully" });
            }

            return BadRequest(new { error = "Failed to revoke API key" });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "API key {ApiKeyId} not found", id);
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to API key {ApiKeyId}", id);
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking API key {ApiKeyId}", id);
            return StatusCode(500, new { error = "An error occurred while revoking the API key" });
        }
    }
}
