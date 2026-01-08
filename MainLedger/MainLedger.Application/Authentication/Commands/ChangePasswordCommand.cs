using MediatR;

namespace MainLedger.Application.Authentication.Commands;

/// <summary>
/// Command to change a user's password (requires old password).
/// </summary>
public record ChangePasswordCommand(
    Guid UserId,
    string OldPassword,
    string NewPassword) : IRequest<bool>;
