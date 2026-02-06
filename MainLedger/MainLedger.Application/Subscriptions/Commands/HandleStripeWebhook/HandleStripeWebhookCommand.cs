using MediatR;

namespace MainLedger.Application.Subscriptions.Commands.HandleStripeWebhook;

/// <summary>
/// Command to handle Stripe webhook events.
/// </summary>
public record HandleStripeWebhookCommand(string Payload, string Signature) : IRequest;
