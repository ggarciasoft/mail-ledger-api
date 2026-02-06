using MediatR;

namespace MainLedger.Application.Authentication.Commands;

/// <summary>
/// Command to authenticate a user via Google OAuth.
/// </summary>
public record GoogleOAuthCommand(
    string Code,
    string? IpAddress = null,
    string? UserAgent = null
) : IRequest<LoginResult>;
