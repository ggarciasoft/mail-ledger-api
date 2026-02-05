using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace MailLedger.PlaygroundProject.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhookReceiverController : ControllerBase
{
    private static readonly List<WebhookLog> _webhookLogs = new();
    private readonly ILogger<WebhookReceiverController> _logger;

    public WebhookReceiverController(ILogger<WebhookReceiverController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Dummy webhook endpoint that always returns 200 OK
    /// </summary>
    [HttpPost("success")]
    public IActionResult ReceiveWebhookSuccess([FromBody] JsonElement payload)
    {
        var log = new WebhookLog
        {
            Id = Guid.NewGuid(),
            Endpoint = "/api/webhookreceiver/success",
            ReceivedAt = DateTime.UtcNow,
            Payload = payload.ToString(),
            StatusCode = 200
        };

        _webhookLogs.Add(log);
        
        _logger.LogInformation("Webhook received at /success: {Payload}", payload.ToString());

        return Ok(new { message = "Webhook received successfully", logId = log.Id });
    }

    /// <summary>
    /// Dummy webhook endpoint that always returns 500 Internal Server Error
    /// </summary>
    [HttpPost("error")]
    public IActionResult ReceiveWebhookError([FromBody] JsonElement payload)
    {
        var log = new WebhookLog
        {
            Id = Guid.NewGuid(),
            Endpoint = "/api/webhookreceiver/error",
            ReceivedAt = DateTime.UtcNow,
            Payload = payload.ToString(),
            StatusCode = 500
        };

        _webhookLogs.Add(log);
        
        _logger.LogError("Webhook received at /error (simulating error): {Payload}", payload.ToString());

        return StatusCode(500, new { message = "Simulated server error", logId = log.Id });
    }

    /// <summary>
    /// Dummy webhook endpoint that returns 200 after 3 attempts (for testing retries)
    /// </summary>
    [HttpPost("retry")]
    public IActionResult ReceiveWebhookRetry([FromBody] JsonElement payload)
    {
        var attemptCount = _webhookLogs.Count(l => l.Endpoint == "/api/webhookreceiver/retry");

        var log = new WebhookLog
        {
            Id = Guid.NewGuid(),
            Endpoint = "/api/webhookreceiver/retry",
            ReceivedAt = DateTime.UtcNow,
            Payload = payload.ToString(),
            StatusCode = attemptCount < 2 ? 500 : 200,
            AttemptNumber = attemptCount + 1
        };

        _webhookLogs.Add(log);

        if (attemptCount < 2)
        {
            _logger.LogWarning("Webhook retry attempt {Attempt} - returning 500", attemptCount + 1);
            return StatusCode(500, new { message = $"Retry attempt {attemptCount + 1} - failing", logId = log.Id });
        }

        _logger.LogInformation("Webhook retry attempt {Attempt} - returning 200", attemptCount + 1);
        return Ok(new { message = $"Success on attempt {attemptCount + 1}", logId = log.Id });
    }

    /// <summary>
    /// Get all received webhook logs
    /// </summary>
    [HttpGet("logs")]
    public IActionResult GetLogs()
    {
        return Ok(_webhookLogs.OrderByDescending(l => l.ReceivedAt));
    }

    /// <summary>
    /// Clear all webhook logs
    /// </summary>
    [HttpDelete("logs")]
    public IActionResult ClearLogs()
    {
        var count = _webhookLogs.Count;
        _webhookLogs.Clear();
        _logger.LogInformation("Cleared {Count} webhook logs", count);
        return Ok(new { message = $"Cleared {count} logs" });
    }

    /// <summary>
    /// Validate webhook signature (dummy implementation)
    /// </summary>
    [HttpPost("validate-signature")]
    public IActionResult ValidateSignature(
        [FromBody] JsonElement payload,
        [FromHeader(Name = "X-Webhook-Signature")] string? signature)
    {
        var log = new WebhookLog
        {
            Id = Guid.NewGuid(),
            Endpoint = "/api/webhookreceiver/validate-signature",
            ReceivedAt = DateTime.UtcNow,
            Payload = payload.ToString(),
            Signature = signature,
            StatusCode = 200
        };

        _webhookLogs.Add(log);

        if (string.IsNullOrEmpty(signature))
        {
            _logger.LogWarning("Webhook received without signature");
            return BadRequest(new { message = "Missing signature header" });
        }

        _logger.LogInformation("Webhook received with signature: {Signature}", signature);
        return Ok(new { message = "Signature validated", signature, logId = log.Id });
    }
}

public class WebhookLog
{
    public Guid Id { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
    public string Payload { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public int? AttemptNumber { get; set; }
    public string? Signature { get; set; }
}
