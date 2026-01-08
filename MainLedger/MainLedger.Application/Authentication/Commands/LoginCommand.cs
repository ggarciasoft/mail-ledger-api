using MediatR;

namespace MainLedger.Application.Authentication.Commands;

/// <summary>
/// Command to authenticate a user and generate tokens.
/// </summary>
public record LoginCommand(
    string Email,
    string Password,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<LoginResult>;

/// <summary>
/// Result of a successful login.
/// </summary>
public record LoginResult(
    Guid UserId,
    string AccessToken,
    string RefreshToken,
    int ExpiresIn);
