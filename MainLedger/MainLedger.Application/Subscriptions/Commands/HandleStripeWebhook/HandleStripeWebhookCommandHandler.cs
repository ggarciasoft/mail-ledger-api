using MainLedger.Application.Common.Interfaces;
using MainLedger.Domain.Settings;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MainLedger.Application.Subscriptions.Commands.HandleStripeWebhook;

/// <summary>
/// Handler for processing Stripe webhook events.
/// </summary>
public class HandleStripeWebhookCommandHandler : IRequestHandler<HandleStripeWebhookCommand>
{
    private readonly IPaymentProvider _paymentProvider;
    private readonly StripeSettings _stripeSettings;
    private readonly ILogger<HandleStripeWebhookCommandHandler> _logger;

    public HandleStripeWebhookCommandHandler(
        IPaymentProvider paymentProvider,
        IOptions<StripeSettings> stripeSettings,
        ILogger<HandleStripeWebhookCommandHandler> logger
    )
    {
        _paymentProvider = paymentProvider;
        _stripeSettings = stripeSettings.Value;
        _logger = logger;
    }

    public async Task Handle(
        HandleStripeWebhookCommand request,
        CancellationToken cancellationToken
    )
    {
        // Validate webhook signature
        var isValid = await _paymentProvider.ValidateWebhookSignatureAsync(
            request.Payload,
            request.Signature,
            _stripeSettings.WebhookSecret
        );

        if (!isValid)
        {
            _logger.LogWarning("Invalid Stripe webhook signature");
            throw new UnauthorizedAccessException("Invalid webhook signature");
        }

        // Process the webhook event
        await _paymentProvider.HandleWebhookEventAsync(request.Payload, cancellationToken);
    }
}
