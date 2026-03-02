using Granit.Encryption;
using Microsoft.Extensions.Options;

namespace Granit.Vault.Providers;

/// <summary>
/// Provider de chiffrement via Vault Transit Engine.
/// Réservé aux opérations rares à haute sécurité.
/// </summary>
public sealed class VaultStringEncryptionProvider(
    ITransitEncryptionService transitEncryption,
    IOptions<StringEncryptionOptions> options) : IStringEncryptionProvider
{
    private readonly string _keyName = options.Value.VaultKeyName;

    /// <inheritdoc/>
    public string ProviderName => StringEncryptionOptions.VaultProviderName;

    /// <inheritdoc/>
    public string Encrypt(string plainText) =>
        transitEncryption.EncryptAsync(_keyName, plainText)
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
            return transitEncryption.DecryptAsync(_keyName, cipherText)
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception)
        {
            return null;
        }
    }
}
