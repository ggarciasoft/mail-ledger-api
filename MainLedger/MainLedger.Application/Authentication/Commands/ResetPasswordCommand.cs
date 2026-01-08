using MediatR;

namespace MainLedger.Application.Authentication.Commands;

/// <summary>
/// Command to reset a user's password using a reset token.
/// </summary>
public record ResetPasswordCommand(string Token, string NewPassword) : IRequest<bool>;
