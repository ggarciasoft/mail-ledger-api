using MainLedger.Application.Common.Interfaces;
using MainLedger.Domain.Repositories;
using MainLedger.Domain.Settings;
using MediatR;
using Microsoft.Extensions.Options;

namespace MainLedger.Application.Subscriptions.Commands.CreateCheckoutSession;

/// <summary>
/// Handler for creating Stripe Checkout sessions.
/// </summary>
public class CreateCheckoutSessionCommandHandler
    : IRequestHandler<CreateCheckoutSessionCommand, string>
{
    private readonly IPaymentProvider _paymentProvider;
    private readonly ISubscriptionPlanRepository _planRepository;
    private readonly IUserRepository _userRepository;
    private readonly StripeSettings _stripeSettings;

    public CreateCheckoutSessionCommandHandler(
        IPaymentProvider paymentProvider,
        ISubscriptionPlanRepository planRepository,
        IUserRepository userRepository,
        IOptions<StripeSettings> stripeSettings
    )
    {
        _paymentProvider = paymentProvider;
        _planRepository = planRepository;
        _userRepository = userRepository;
        _stripeSettings = stripeSettings.Value;
    }

    public async Task<string> Handle(
        CreateCheckoutSessionCommand request,
        CancellationToken cancellationToken
    )
    {
        // Get user
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {request.UserId} not found");
        }

        // Get plan
        var plan = await _planRepository.GetByIdAsync(request.PlanId, cancellationToken);
        if (plan == null)
        {
            throw new KeyNotFoundException($"Plan with ID {request.PlanId} not found");
        }

        // Validate plan has Stripe Price ID
        if (string.IsNullOrEmpty(plan.StripePriceId))
        {
            throw new InvalidOperationException(
                $"Plan {plan.Name} does not have a Stripe Price ID configured"
            );
        }

        // Create metadata for tracking
        var metadata = new Dictionary<string, string>
        {
            { "userId", request.UserId.ToString() },
            { "planId", request.PlanId.ToString() },
            { "planName", plan.Name },
        };

        // Create checkout session
        var checkoutUrl = await _paymentProvider.CreateCheckoutSessionAsync(
            user.Email.Value,
            plan.StripePriceId,
            _stripeSettings.CheckoutSuccessUrl,
            _stripeSettings.CheckoutCancelUrl,
            metadata,
            cancellationToken
        );

        return checkoutUrl;
    }
}
