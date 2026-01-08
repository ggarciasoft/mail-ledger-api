namespace MainLedger.Contracts.Authentication;

/// <summary>
/// Request to login.
/// </summary>
public record LoginRequest(
    string Email,
    string Password);

/// <summary>
/// Response after successful login.
/// </summary>
public record LoginResponse(
    Guid UserId,
    string AccessToken,
    string RefreshToken,
    int ExpiresIn);
