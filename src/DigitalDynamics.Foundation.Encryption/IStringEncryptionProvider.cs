namespace DigitalDynamics.Foundation.Encryption;

/// <summary>
/// String encryption/decryption provider.
/// Each implementation manages its own configuration via DI.
/// </summary>
public interface IStringEncryptionProvider
{
    /// <summary>Provider name (e.g. "Aes", "Vault").</summary>
    string ProviderName { get; }

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
