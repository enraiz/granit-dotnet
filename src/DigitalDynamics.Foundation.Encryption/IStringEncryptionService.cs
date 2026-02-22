// =============================================================================
// IStringEncryptionService - String encryption/decryption
// =============================================================================
// Primary abstraction for string encryption.
// Delegates to the provider selected via StringEncryptionOptions.ProviderName.
//
// Implementation: DefaultStringEncryptionService (in this package).
// Available providers: AES-256-CBC (local) or Vault Transit (remote).
// =============================================================================

namespace DigitalDynamics.Foundation.Encryption;

/// <summary>
/// String encryption/decryption service.
/// </summary>
public interface IStringEncryptionService
{
    /// <summary>
    /// Encrypts a plain-text string.
    /// </summary>
    /// <param name="plainText">Plain-text string to encrypt.</param>
    /// <returns>Encrypted text encoded as Base64.</returns>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypts an encrypted string.
    /// </summary>
    /// <param name="cipherText">Encrypted text encoded as Base64.</param>
    /// <returns>Plain-text string, or <c>null</c> if decryption fails.</returns>
    string? Decrypt(string cipherText);
}
