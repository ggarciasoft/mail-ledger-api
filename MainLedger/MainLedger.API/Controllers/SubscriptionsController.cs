using MainLedger.Application.Authentication.Services;
using MainLedger.Application.Subscriptions.Commands.CancelSubscription;
using MainLedger.Application.Subscriptions.Commands.CreateCheckoutSession;
using MainLedger.Application.Subscriptions.Commands.HandleStripeWebhook;
using MainLedger.Application.Subscriptions.Commands.UpgradeSubscription;
using MainLedger.Application.Subscriptions.Queries.GetSubscriptionPlans;
using MainLedger.Application.Subscriptions.Queries.GetSubscriptionUsage;
using MainLedger.Application.Subscriptions.Queries.GetUserSubscription;
using MainLedger.Contracts.Subscriptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MainLedger.API.Controllers;

/// <summary>
/// Controller for subscription management operations.
/// </summary>
[ApiController]
[Route("api/subscriptions")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<SubscriptionsController> _logger;

    public SubscriptionsController(
        IMediator mediator,
        ICurrentUserService currentUserService,
        ILogger<SubscriptionsController> logger
    )
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Get all available subscription plans.
    /// </summary>
    [HttpGet("plans")]
    [AllowAnonymous] // Allow anonymous to view pricing
    public async Task<ActionResult<List<SubscriptionPlanDto>>> GetPlans(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var query = new GetSubscriptionPlansQuery();
            var plans = await _mediator.Send(query, cancellationToken);
            return Ok(plans);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription plans");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get the current user's subscription.
    /// </summary>
    [HttpGet("my-subscription")]
    public async Task<ActionResult<UserSubscriptionDto>> GetMySubscription(
        CancellationToken cancellationToken = default
    )
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "User not authenticated" });
        }

        try
        {
            var query = new GetUserSubscriptionQuery(userId.Value);
            var subscription = await _mediator.Send(query, cancellationToken);

            if (subscription == null)
            {
                return NotFound(new { error = "No subscription found" });
            }

            return Ok(subscription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user subscription for user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get the current user's subscription usage statistics.
    /// </summary>
    [HttpGet("usage")]
    public async Task<ActionResult<SubscriptionUsageDto>> GetUsage(
        CancellationToken cancellationToken = default
    )
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "User not authenticated" });
        }

        try
        {
            var query = new GetSubscriptionUsageQuery(userId.Value);
            var usage = await _mediator.Send(query, cancellationToken);
            return Ok(usage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription usage for user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Upgrade the current user's subscription to a new plan.
    /// </summary>
    [HttpPost("upgrade")]
    public async Task<IActionResult> Upgrade(
        [FromBody] UpgradeSubscriptionRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "User not authenticated" });
        }

        try
        {
            var command = new UpgradeSubscriptionCommand(userId.Value, request.PlanId);
            await _mediator.Send(command, cancellationToken);

            return Ok(new { success = true, message = "Subscription upgraded successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upgrading subscription for user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Cancel the current user's subscription.
    /// </summary>
    [HttpPost("cancel")]
    public async Task<IActionResult> Cancel(
        [FromBody] CancelSubscriptionRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "User not authenticated" });
        }

        try
        {
            var command = new CancelSubscriptionCommand(userId.Value, request.Reason);
            await _mediator.Send(command, cancellationToken);

            return Ok(
                new
                {
                    success = true,
                    message = "Subscription cancelled. You will retain access until the end of your current billing period.",
                }
            );
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling subscription for user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Create a Stripe Checkout session for subscription payment.
    /// </summary>
    [HttpPost("checkout")]
    public async Task<ActionResult<CreateCheckoutSessionResponse>> CreateCheckoutSession(
        [FromBody] CreateCheckoutSessionRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "User not authenticated" });
        }

        try
        {
            var command = new CreateCheckoutSessionCommand(userId.Value, request.PlanId);
            var checkoutUrl = await _mediator.Send(command, cancellationToken);

            return Ok(new CreateCheckoutSessionResponse(checkoutUrl));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating checkout session for user {UserId}", userId);
            return BadRequest(new { error = "Failed to create checkout session" });
        }
    }

    /// <summary>
    /// Stripe webhook endpoint for subscription events.
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous] // Webhooks come from Stripe, not authenticated users
    public async Task<IActionResult> StripeWebhook(CancellationToken cancellationToken = default)
    {
        try
        {
            // Read raw body
            using var reader = new StreamReader(HttpContext.Request.Body);
            var payload = await reader.ReadToEndAsync(cancellationToken);

            // Get Stripe signature header
            var signature = Request.Headers["Stripe-Signature"].ToString();

            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("Webhook received without Stripe-Signature header");
                return BadRequest(new { error = "Missing Stripe-Signature header" });
            }

            // Process webhook
            var command = new HandleStripeWebhookCommand(payload, signature);
            await _mediator.Send(command, cancellationToken);

            return Ok();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized webhook attempt");
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook");
            return BadRequest(new { error = "Failed to process webhook" });
        }
    }
}
