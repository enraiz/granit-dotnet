namespace Granit.Vault;

/// <summary>
/// Transit encryption/decryption service for protecting sensitive data at rest.
/// Implemented by provider-specific packages (HashiCorp Vault Transit, Azure Key Vault, AWS KMS).
/// </summary>
public interface ITransitEncryptionService
{
    /// <summary>
    /// Encrypts plaintext using the configured transit engine.
    /// </summary>
    /// <param name="keyName">Logical key name (e.g. "sensitive-data").</param>
    /// <param name="plaintext">Plaintext to encrypt.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Provider-specific ciphertext (e.g. "vault:v1:..." for HashiCorp, Base64 for Azure/AWS).</returns>
    Task<string> EncryptAsync(string keyName, string plaintext, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts ciphertext using the configured transit engine.
    /// </summary>
    /// <param name="keyName">Logical key name (e.g. "sensitive-data").</param>
    /// <param name="ciphertext">Provider-specific ciphertext.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Decrypted plaintext.</returns>
    Task<string> DecryptAsync(string keyName, string ciphertext, CancellationToken cancellationToken = default);
}
