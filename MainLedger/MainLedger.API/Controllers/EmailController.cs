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
public class EmailController : ControllerBase
{
    private readonly IMediator _mediator;

    public EmailController(IMediator mediator)
    {
        _mediator = mediator;
    }

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
    [HttpPost("{provider}/callback")]
    public async Task<IActionResult> HandleCallback(
        [FromRoute] EmailProvider provider,
        [FromBody] ConnectProviderRequest request
    )
    {
        var result = await _mediator.Send(new ConnectEmailProviderCommand(provider, request.Code));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
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
