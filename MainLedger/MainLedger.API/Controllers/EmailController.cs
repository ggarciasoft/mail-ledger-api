using MainLedger.Application.Email.Commands;
using MainLedger.Application.Email.Queries;
using MainLedger.Contracts.Email;
using MainLedger.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MainLedger.API.Controllers;

[ApiController]
[Route("api/email")]
[Authorize]
public class EmailController(IMediator _mediator, ILogger<EmailController> _logger, IConfiguration _configuration) : ControllerBase
{
    /// <summary>
    /// Get OAuth authorization URL for email provider
    /// </summary>
    [HttpGet("{provider}/auth-url")]
    public async Task<IActionResult> GetAuthUrl([FromRoute] EmailProvider provider)
    {
        var result = await _mediator.Send(new GetAuthUrlQuery(provider));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    /// <summary>
    /// Handle OAuth callback and connect email provider
    /// </summary>
    [HttpGet("{provider}/callback")]
    [AllowAnonymous] // Allow anonymous for OAuth callback
    public async Task<IActionResult> HandleCallback(
        [FromRoute] EmailProvider provider,
        [FromQuery] string? code = null,
        [FromQuery] string? state = null,
        [FromQuery] string? error = null,
        [FromQuery] string? error_description = null
    )
    {
        try
        {
            // Check for OAuth errors
            if (!string.IsNullOrEmpty(error))
            {
                _logger.LogError("OAuth error: {Error} - {Description}", error, error_description);
                return Redirect($"{_configuration["FrontendUrl"]}/integrations?error={Uri.EscapeDataString(error_description ?? error)}");
            }

            // Validate required parameters
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            {
                _logger.LogError("Missing required parameters: code or state");
                return Redirect($"{_configuration["FrontendUrl"]}/integrations?error=invalid_callback_parameters");
            }

            var result = await _mediator.Send(new ConnectEmailProviderCommand(provider, code, state));

            if (!result.IsSuccess)
            {
                _logger.LogError("Failed to connect {Provider}: {Error}", provider, result.Error);
                return Redirect($"{_configuration["FrontendUrl"]}/integrations?error=email_connection_failed");
            }

            // Redirect to settings page with success
            return Redirect($"{_configuration["FrontendUrl"]}/integrations?email_connected=true");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling {Provider} callback", provider);
            // Redirect to settings with error
            return Redirect($"{_configuration["FrontendUrl"]}/integrations?error=email_connection_failed");
        }
    }

    /// <summary>
    /// Handle OAuth callback and connect email provider
    /// </summary>
    [HttpPost("{provider}/callback")]
    [AllowAnonymous] // Allow anonymous for OAuth callback
    public async Task<IActionResult> HandleCallback(
        [FromRoute] EmailProvider provider,
        [FromBody] ConnectProviderRequest request
    )
    {
        try
        {
            var result = await _mediator.Send(new ConnectEmailProviderCommand(provider, request.Code, request.State));

            // Redirect to settings page with success
            return Redirect($"{_configuration["FrontendUrl"]}/integrations?email_connected=true");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling {provider} callback", provider);
            // Redirect to settings with error
            return Redirect($"{_configuration["FrontendUrl"]}/integrations?error=email_connection_failed");
        }
    }

    /// <summary>
    /// Trigger email sync for provider
    /// </summary>
    [HttpPost("{provider}/sync")]
    public async Task<IActionResult> SyncEmails(
        [FromRoute] EmailProvider provider,
        [FromBody] SyncEmailsRequest? request = null
    )
    {
        var result = await _mediator.Send(
            new SyncEmailsCommand(provider, request?.SyncFrom, request?.MaxResults)
        );
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    /// <summary>
    /// Get all email connections for current user
    /// </summary>
    [HttpGet("connections")]
    public async Task<IActionResult> GetConnections()
    {
        var result = await _mediator.Send(new GetEmailConnectionsQuery());
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    /// <summary>
    /// Disconnect email provider
    /// </summary>
    [HttpDelete("connections/{provider}")]
    public async Task<IActionResult> Disconnect([FromRoute] EmailProvider provider)
    {
        var result = await _mediator.Send(new DisconnectEmailProviderCommand(provider));
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }
}
