using MediatR;

namespace MainLedger.Application.Authentication.Commands;

/// <summary>
/// Command to verify a user's email address.
/// </summary>
public record VerifyEmailCommand(string Token) : IRequest<bool>;
