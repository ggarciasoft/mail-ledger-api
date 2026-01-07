namespace MainLedger.Application.Common.Interfaces;

/// <summary>
/// Interface for encrypting and decrypting sensitive tokens.
/// </summary>
public interface ITokenEncryptionService
{
    /// <summary>
    /// Encrypts a plaintext token.
    /// </summary>
    /// <param name="plaintext">The plaintext token to encrypt.</param>
    /// <returns>The encrypted token.</returns>
    string Encrypt(string plaintext);

    /// <summary>
    /// Decrypts an encrypted token.
    /// </summary>
    /// <param name="ciphertext">The encrypted token to decrypt.</param>
    /// <returns>The decrypted plaintext token.</returns>
    string Decrypt(string ciphertext);
}

