using Granit.Encryption;
using Granit.Encryption.Options;
using Microsoft.Extensions.Options;

namespace Granit.Vault.HashiCorp.Providers;

/// <summary>
/// String encryption provider backed by HashiCorp Vault Transit Engine.
/// Reserved for rare, high-security operations.
/// </summary>
public sealed class HashiCorpVaultStringEncryptionProvider(
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
