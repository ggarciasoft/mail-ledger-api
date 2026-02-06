using MediatR;

namespace MainLedger.Application.Authentication.Commands;

/// <summary>
/// Command to get OAuth authorization URL.
/// </summary>
public record GetOAuthUrlCommand(
    string Provider,
    string State
) : IRequest<string>;
