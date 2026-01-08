using MediatR;

namespace MainLedger.Application.Authentication.Commands;

/// <summary>
/// Command to register a new user.
/// </summary>
public record RegisterUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName) : IRequest<Guid>;
