using MainLedger.Domain.Services;
using System.Security.Cryptography;

namespace MainLedger.Infrastructure.Security;

/// <summary>
/// Cryptographically secure token generator implementation.
/// </summary>
public class TokenGenerator : ITokenGenerator
{
    /// <summary>
    /// Generates a cryptographically secure random token.
    /// </summary>
    public string GenerateToken(int length = 32)
    {
        if (length <= 0)
            throw new ArgumentException("Length must be greater than zero.", nameof(length));

        var bytes = new byte[length];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }

        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Generates an email verification token.
    /// </summary>
    public string GenerateEmailVerificationToken()
    {
        return GenerateToken(32);
    }

    /// <summary>
    /// Generates a password reset token.
    /// </summary>
    public string GeneratePasswordResetToken()
    {
        return GenerateToken(32);
    }

    /// <summary>
    /// Generates a refresh token for JWT authentication.
    /// </summary>
    public string GenerateRefreshToken()
    {
        return GenerateToken(64);
    }
}
