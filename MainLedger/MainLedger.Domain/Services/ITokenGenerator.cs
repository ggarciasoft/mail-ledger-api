namespace MainLedger.Domain.Services;

/// <summary>
/// Domain service for generating cryptographically secure tokens.
/// Implementation should be provided by Infrastructure layer.
/// </summary>
public interface ITokenGenerator
{
    /// <summary>
    /// Generates a cryptographically secure random token.
    /// </summary>
    /// <param name="length">Length of the token in bytes (default: 32)</param>
    /// <returns>Base64-encoded token string</returns>
    string GenerateToken(int length = 32);

    /// <summary>
    /// Generates an email verification token.
    /// </summary>
    string GenerateEmailVerificationToken();

    /// <summary>
    /// Generates a password reset token.
    /// </summary>
    string GeneratePasswordResetToken();

    /// <summary>
    /// Generates a refresh token for JWT authentication.
    /// </summary>
    string GenerateRefreshToken();
}
