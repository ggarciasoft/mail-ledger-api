using MainLedger.Application.Common.Interfaces;
using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using MainLedger.Domain.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace MainLedger.Infrastructure.Payment;

/// <summary>
/// Stripe implementation of the payment provider interface.
/// </summary>
public class StripePaymentProvider : IPaymentProvider
{
    private readonly StripeSettings _settings;
    private readonly IUserSubscriptionRepository _subscriptionRepository;
    private readonly ISubscriptionPlanRepository _planRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<StripePaymentProvider> _logger;

    public StripePaymentProvider(
        IOptions<StripeSettings> settings,
        IUserSubscriptionRepository subscriptionRepository,
        ISubscriptionPlanRepository planRepository,
        IUnitOfWork unitOfWork,
        ILogger<StripePaymentProvider> logger
    )
    {
        _settings = settings.Value;
        _subscriptionRepository = subscriptionRepository;
        _planRepository = planRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<string> CreateCheckoutSessionAsync(
        string userEmail,
        string planPriceId,
        string successUrl,
        string cancelUrl,
        Dictionary<string, string> metadata,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var options = new SessionCreateOptions
            {
                Mode = "subscription",
                CustomerEmail = userEmail,
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Price = planPriceId,
                        Quantity = 1,
                    },
                },
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                Metadata = metadata,
                SubscriptionData = new SessionSubscriptionDataOptions
                {
                    Metadata = metadata,
                },
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options, cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Created Stripe Checkout session {SessionId} for user {UserEmail}",
                session.Id,
                userEmail
            );

            return session.Url;
        }
        catch (StripeException ex)
        {
            _logger.LogError(
                ex,
                "Failed to create Stripe Checkout session for user {UserEmail}",
                userEmail
            );
            throw new InvalidOperationException("Failed to create checkout session", ex);
        }
    }

    public Task<bool> ValidateWebhookSignatureAsync(
        string payload,
        string signature,
        string secret
    )
    {
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(payload, signature, secret);
            return Task.FromResult(true);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Invalid webhook signature");
            return Task.FromResult(false);
        }
    }

    public async Task HandleWebhookEventAsync(
        string eventJson,
        CancellationToken cancellationToken = default
    )
    {
        var stripeEvent = EventUtility.ParseEvent(eventJson);

        _logger.LogInformation(
            "Processing Stripe webhook event {EventType} with ID {EventId}",
            stripeEvent.Type,
            stripeEvent.Id
        );

        // For now, just log the event. We'll implement full webhook handling after testing with real Stripe events
        // to determine the correct property names for the Stripe.NET library version being used.
        switch (stripeEvent.Type)
        {
            case "checkout.session.completed":
                await HandleCheckoutSessionCompletedAsync(stripeEvent, cancellationToken);
                break;

            default:
                _logger.LogInformation(
                    "Unhandled Stripe event type: {EventType}. Event data: {EventData}",
                    stripeEvent.Type,
                    System.Text.Json.JsonSerializer.Serialize(stripeEvent.Data.Object)
                );
                break;
        }
    }

    private async Task HandleCheckoutSessionCompletedAsync(
        Event stripeEvent,
        CancellationToken cancellationToken
    )
    {
        var session = stripeEvent.Data.Object as Session;
        if (session == null)
        {
            _logger.LogWarning("Checkout session event has no session data");
            return;
        }

        if (!session.Metadata.ContainsKey("userId") || !session.Metadata.ContainsKey("planId"))
        {
            _logger.LogWarning(
                "Checkout session {SessionId} missing userId or planId in metadata",
                session.Id
            );
            return;
        }

        var userId = Guid.Parse(session.Metadata["userId"]);
        var planId = Guid.Parse(session.Metadata["planId"]);

        // Get or create subscription
        var subscription = await _subscriptionRepository.GetByUserIdAsync(
            userId,
            cancellationToken
        );

        if (subscription == null)
        {
            subscription = new UserSubscription(userId, planId, PaymentProvider.Stripe);
            _subscriptionRepository.Add(subscription);
        }
        else
        {
            subscription.Upgrade(planId);
        }

        subscription.SetStripeCustomerId(session.CustomerId);
        subscription.SetStripeSubscriptionId(session.SubscriptionId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Activated subscription for user {UserId} via checkout session {SessionId}",
            userId,
            session.Id
        );
    }
}
