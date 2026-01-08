namespace MainLedger.Contracts.Authentication;

/// <summary>
/// Request to register a new user.
/// </summary>
public record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName);

/// <summary>
/// Response after successful registration.
/// </summary>
public record RegisterResponse(
    Guid UserId,
    string Email,
    string Message);
