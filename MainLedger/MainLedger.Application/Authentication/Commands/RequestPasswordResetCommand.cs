using MediatR;

namespace MainLedger.Application.Authentication.Commands;

/// <summary>
/// Command to request a password reset.
/// </summary>
public record RequestPasswordResetCommand(string Email) : IRequest<bool>;
