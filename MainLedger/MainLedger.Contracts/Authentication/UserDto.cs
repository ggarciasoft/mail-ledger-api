namespace MainLedger.Contracts.Authentication;

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
