using MediatR;

namespace MainLedger.Application.Authentication.Commands;

/// <summary>
/// Command to authenticate a user via Microsoft OAuth.
/// </summary>
public record MicrosoftOAuthCommand(
    string Code,
    string? IpAddress = null,
    string? UserAgent = null
) : IRequest<LoginResult>;
