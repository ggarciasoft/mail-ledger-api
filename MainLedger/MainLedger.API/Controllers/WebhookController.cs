using MainLedger.Application.Webhooks.Commands;
using MainLedger.Application.Webhooks.Queries;
using MainLedger.Contracts.Webhooks;
using MainLedger.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MainLedger.API.Controllers;

[ApiController]
[Route("api/webhooks")]
[Authorize]
public class WebhookController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(IMediator mediator, ILogger<WebhookController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }

    /// <summary>
    /// Get all webhook endpoints for the current user
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<Contracts.Webhooks.WebhookEndpointDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWebhookEndpoints()
    {
        var userId = GetUserId();
        var query = new GetWebhookEndpointsQuery(userId);
        var endpoints = await _mediator.Send(query);

        var dtos = endpoints.Select(e => new Contracts.Webhooks.WebhookEndpointDto(
            e.Id,
            e.Url,
            e.Events,
            e.IsActive,
            e.CreatedAt,
            e.LastTriggeredAt
        )).ToList();

        return Ok(dtos);
    }

    /// <summary>
    /// Create a new webhook endpoint
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Contracts.Webhooks.WebhookEndpointDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateWebhookEndpoint([FromBody] CreateWebhookEndpointRequest request)
    {
        var userId = GetUserId();

        // Parse event types
        var eventTypes = new List<WebhookEventType>();
        foreach (var eventStr in request.Events)
        {
            if (Enum.TryParse<WebhookEventType>(eventStr, true, out var eventType))
            {
                eventTypes.Add(eventType);
            }
            else
            {
                return BadRequest($"Invalid event type: {eventStr}");
            }
        }

        var command = new CreateWebhookEndpointCommand(userId, request.Url, eventTypes);
        var result = await _mediator.Send(command);

        var dto = new Contracts.Webhooks.WebhookEndpointDto(
            result.Id,
            request.Url,
            request.Events,
            true,
            DateTime.UtcNow,
            null,
            result.SecretKey // Only returned on creation
        );

        return CreatedAtAction(nameof(GetWebhookEndpoints), new { id = result.Id }, dto);
    }

    /// <summary>
    /// Update an existing webhook endpoint
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateWebhookEndpoint(Guid id, [FromBody] UpdateWebhookEndpointRequest request)
    {
        var userId = GetUserId();

        // Parse event types
        var eventTypes = new List<WebhookEventType>();
        foreach (var eventStr in request.Events)
        {
            if (Enum.TryParse<WebhookEventType>(eventStr, true, out var eventType))
            {
                eventTypes.Add(eventType);
            }
            else
            {
                return BadRequest($"Invalid event type: {eventStr}");
            }
        }

        var command = new UpdateWebhookEndpointCommand(id, userId, request.Url, eventTypes);
        await _mediator.Send(command);

        return NoContent();
    }

    /// <summary>
    /// Delete a webhook endpoint
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteWebhookEndpoint(Guid id)
    {
        var userId = GetUserId();
        var command = new DeleteWebhookEndpointCommand(id, userId);
        await _mediator.Send(command);

        return NoContent();
    }

    /// <summary>
    /// Toggle webhook endpoint active status
    /// </summary>
    [HttpPatch("{id}/toggle")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleWebhookEndpoint(Guid id, [FromBody] bool isActive)
    {
        var userId = GetUserId();
        var command = new ToggleWebhookEndpointCommand(id, userId, isActive);
        await _mediator.Send(command);

        return NoContent();
    }

    /// <summary>
    /// Get delivery history for a webhook endpoint
    /// </summary>
    [HttpGet("{id}/deliveries")]
    [ProducesResponseType(typeof(GetWebhookDeliveriesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWebhookDeliveries(
        Guid id,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetUserId();

        WebhookDeliveryStatus? deliveryStatus = null;
        if (!string.IsNullOrEmpty(status))
        {
            if (Enum.TryParse<WebhookDeliveryStatus>(status, true, out var parsedStatus))
            {
                deliveryStatus = parsedStatus;
            }
            else
            {
                return BadRequest($"Invalid status: {status}");
            }
        }

        var query = new GetWebhookDeliveriesQuery(id, userId, deliveryStatus, fromDate, toDate, page, pageSize);
        var result = await _mediator.Send(query);

        var response = new GetWebhookDeliveriesResponse(
            result.Deliveries.Select(d => new Contracts.Webhooks.WebhookDeliveryDto(
                d.Id,
                d.WebhookEndpointId,
                d.EventType,
                d.Status,
                d.AttemptCount,
                d.LastAttemptAt,
                d.ResponseStatusCode,
                d.ErrorMessage,
                d.CreatedAt
            )).ToList(),
            result.TotalCount,
            result.Page,
            result.PageSize
        );

        return Ok(response);
    }
}
