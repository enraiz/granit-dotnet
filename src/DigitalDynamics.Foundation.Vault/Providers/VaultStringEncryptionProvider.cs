// =============================================================================
// VaultStringEncryptionProvider - Chiffrement via Vault Transit Engine
// =============================================================================
// Implémente IStringEncryptionProvider en déléguant à ITransitEncryptionService.
// Utilise l'engine Transit de Vault pour les opérations rares à haute sécurité.
//
// Latence : 10-20 ms (appel réseau Vault). À réserver pour les cas critiques.
// Pour les opérations fréquentes, préférer AesStringEncryptionProvider (< 1 ms).
//
// Inputs  : plainText/cipherText (string), configuration via IOptions<StringEncryptionOptions>
// Outputs : string vault:v1:... (encrypt) | plaintext | null si erreur (decrypt)
// =============================================================================

using DigitalDynamics.Foundation.Encryption;
using Microsoft.Extensions.Options;

namespace DigitalDynamics.Foundation.Vault.Providers;

/// <summary>
/// Provider de chiffrement via Vault Transit Engine.
/// Réservé aux opérations rares à haute sécurité.
/// </summary>
public sealed class VaultStringEncryptionProvider : IStringEncryptionProvider
{
    private readonly ITransitEncryptionService _transitEncryption;
    private readonly string _keyName;

    /// <inheritdoc/>
    public string ProviderName => StringEncryptionOptions.VaultProviderName;

    public VaultStringEncryptionProvider(
        ITransitEncryptionService transitEncryption,
        IOptions<StringEncryptionOptions> options)
    {
        _transitEncryption = transitEncryption;
        _keyName = options.Value.VaultKeyName;
    }

    /// <inheritdoc/>
    public string Encrypt(string plainText) =>
        _transitEncryption.EncryptAsync(_keyName, plainText)
            .GetAwaiter()
            .GetResult();

    /// <inheritdoc/>
    public string? Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
        {
            return null;
        }

        try
        {
            return _transitEncryption.DecryptAsync(_keyName, cipherText)
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception)
        {
            return null;
        }
    }
}
