namespace MainLedger.Application.Authentication.Services;

/// <summary>
/// Service for generating and validating JWT tokens.
/// Implementation should be provided by Infrastructure layer.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a JWT access token for a user.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="email">User email</param>
    /// <param name="scopes">Permission scopes</param>
    /// <returns>JWT token string</returns>
    string GenerateAccessToken(Guid userId, string email, string[] scopes);

    /// <summary>
    /// Validates a JWT token and returns the user ID if valid.
    /// </summary>
    /// <param name="token">JWT token to validate</param>
    /// <returns>User ID if valid, null otherwise</returns>
    Guid? ValidateToken(string token);
}
