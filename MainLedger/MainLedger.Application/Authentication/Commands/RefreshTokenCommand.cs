using MediatR;

namespace MainLedger.Application.Authentication.Commands;

/// <summary>
/// Command to refresh an access token using a refresh token.
/// </summary>
public record RefreshTokenCommand(string RefreshToken) : IRequest<LoginResult>;
