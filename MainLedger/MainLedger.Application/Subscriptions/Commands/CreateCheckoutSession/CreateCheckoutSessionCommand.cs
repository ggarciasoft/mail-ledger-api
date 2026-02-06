using MediatR;

namespace MainLedger.Application.Subscriptions.Commands.CreateCheckoutSession;

/// <summary>
/// Command to create a Stripe Checkout session for subscription payment.
/// </summary>
public record CreateCheckoutSessionCommand(Guid UserId, Guid PlanId) : IRequest<string>;
