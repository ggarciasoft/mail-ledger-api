using MainLedger.Application.Common.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace MainLedger.Infrastructure.Security;

/// <summary>
/// Implementation of token encryption using ASP.NET Core Data Protection API.
/// Provides secure encryption/decryption of sensitive tokens (e.g., OAuth refresh tokens).
/// </summary>
public class TokenEncryptionService : ITokenEncryptionService
{
    private readonly IDataProtector _protector;

    public TokenEncryptionService(IDataProtectionProvider dataProtectionProvider)
    {
        // Create a protector with a specific purpose string
        // This ensures tokens encrypted for this purpose can't be decrypted by other services
        _protector = dataProtectionProvider.CreateProtector("MailLedger.GmailTokens.v1");
    }

    public string Encrypt(string plaintext)
    {
        if (string.IsNullOrWhiteSpace(plaintext))
        {
            throw new ArgumentException("Plaintext cannot be empty.", nameof(plaintext));
        }

        return _protector.Protect(plaintext);
    }

    public string Decrypt(string ciphertext)
    {
        if (string.IsNullOrWhiteSpace(ciphertext))
        {
            throw new ArgumentException("Ciphertext cannot be empty.", nameof(ciphertext));
        }

        try
        {
            return _protector.Unprotect(ciphertext);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to decrypt token. The token may be corrupted or invalid.", ex);
        }
    }
}

