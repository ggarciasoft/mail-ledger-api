using MainLedger.Domain.Services;

namespace MainLedger.Infrastructure.Security;

/// <summary>
/// BCrypt-based password hasher implementation.
/// Uses BCrypt.Net library for secure password hashing.
/// </summary>
public class PasswordHasher : Domain.Services.IPasswordHasher
{
    /// <summary>
    /// Hashes a plain text password using BCrypt.
    /// </summary>
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty.", nameof(password));

        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    /// <summary>
    /// Verifies a password against a BCrypt hash.
    /// </summary>
    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty.", nameof(password));
        if (string.IsNullOrWhiteSpace(hash))
            throw new ArgumentException("Hash cannot be empty.", nameof(hash));

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }
}

