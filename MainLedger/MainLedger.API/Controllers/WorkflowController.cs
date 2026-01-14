using MainLedger.Application.Authentication.Services;
using MainLedger.Application.Common.Interfaces;
using MainLedger.Contracts.Workflow;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MainLedger.API.Controllers;

/// <summary>
/// Controller for managing workflow automation configuration.
/// </summary>
[ApiController]
[Route("api/workflow")]
[Authorize]
public class WorkflowController : ControllerBase
{
    private readonly IWorkflowService _workflowService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<WorkflowController> _logger;

    public WorkflowController(
        IWorkflowService workflowService,
        ICurrentUserService currentUserService,
        ILogger<WorkflowController> logger
    )
    {
        _workflowService = workflowService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current workflow configuration for the authenticated user.
    /// </summary>
    [HttpGet("configuration")]
    public async Task<ActionResult<WorkflowConfigurationDto>> GetConfiguration(
        CancellationToken cancellationToken
    )
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "User not authenticated" });
        }

        try
        {
            var config = await _workflowService.GetConfigurationAsync(
                userId.Value,
                cancellationToken
            );
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow configuration for user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Updates the workflow configuration for the authenticated user.
    /// </summary>
    [HttpPut("configuration")]
    public async Task<ActionResult> UpdateConfiguration(
        [FromBody] UpdateWorkflowConfigDto dto,
        CancellationToken cancellationToken
    )
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "User not authenticated" });
        }

        try
        {
            await _workflowService.UpdateConfigurationAsync(userId.Value, dto, cancellationToken);

            _logger.LogInformation(
                "Updated workflow configuration for user {UserId} to mode {Mode}",
                userId,
                dto.Mode
            );

            return Ok(new { message = "Workflow configuration updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating workflow configuration for user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }
}
