using MediatR;

namespace MainLedger.Application.Authentication.Queries;

/// <summary>
/// Query to get a user by ID.
/// </summary>
public record GetUserByIdQuery(Guid UserId) : IRequest<UserDto?>;

/// <summary>
/// DTO for user information.
/// </summary>
public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    bool IsEmailVerified,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastLoginAt);
