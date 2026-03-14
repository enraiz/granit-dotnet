namespace Granit.Vault.Aws.Services;

/// <summary>
/// Transit encryption service backed by AWS KMS.
/// Mirrors the <c>ITransitEncryptionService</c> interface from Granit.Vault.
/// </summary>
public interface IKmsTransitEncryptionService
{
    /// <summary>Encrypts plaintext using AWS KMS.</summary>
    /// <param name="keyName">Logical key name (for tracing only — the actual KMS key is configured via <c>AwsVaultOptions.KmsKeyId</c>).</param>
    /// <param name="plaintext">Plain text to encrypt.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Base64-encoded ciphertext.</returns>
    Task<string> EncryptAsync(string keyName, string plaintext, CancellationToken cancellationToken = default);

    /// <summary>Decrypts ciphertext using AWS KMS.</summary>
    /// <param name="keyName">Logical key name (for tracing only).</param>
    /// <param name="ciphertext">Base64-encoded ciphertext.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Decrypted plain text.</returns>
    Task<string> DecryptAsync(string keyName, string ciphertext, CancellationToken cancellationToken = default);
}
