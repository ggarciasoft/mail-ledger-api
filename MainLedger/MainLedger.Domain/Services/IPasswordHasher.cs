namespace MainLedger.Domain.Services;

/// <summary>
/// Domain service for password hashing and verification.
/// Implementation should be provided by Infrastructure layer.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a plain text password.
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a password against a hash.
    /// </summary>
    bool VerifyPassword(string password, string hash);
}
