// =============================================================================
// DefaultStringEncryptionService - Implémentation de IStringEncryptionService
// =============================================================================
// Délègue au provider configuré via StringEncryptionOptions.ProviderName.
// Les providers disponibles sont enregistrés comme IStringEncryptionProvider
// et résolus par nom au démarrage.
//
// Inputs  : providers (IEnumerable<IStringEncryptionProvider>),
//           options (IOptions<StringEncryptionOptions>)
// Outputs : string (Encrypt) | string? (Decrypt)
// =============================================================================

using DigitalDynamics.Foundation.Encryption;
using Microsoft.Extensions.Options;

namespace DigitalDynamics.Foundation.Encryption.Services;

/// <summary>
/// Implémentation de <see cref="IStringEncryptionService"/> qui délègue
/// au <see cref="IStringEncryptionProvider"/> sélectionné par configuration.
/// </summary>
public sealed class DefaultStringEncryptionService : IStringEncryptionService
{
    private readonly IStringEncryptionProvider _provider;

    public DefaultStringEncryptionService(
        IEnumerable<IStringEncryptionProvider> providers,
        IOptions<StringEncryptionOptions> options)
    {
        string providerName = options.Value.ProviderName;

        _provider = providers.FirstOrDefault(p => p.ProviderName == providerName)
            ?? throw new InvalidOperationException(
                $"Aucun provider de chiffrement nommé '{providerName}' n'est enregistré. " +
                $"Providers disponibles : {string.Join(", ", providers.Select(p => p.ProviderName))}.");
    }

    /// <inheritdoc/>
    public string Encrypt(string plainText) => _provider.Encrypt(plainText);

    /// <inheritdoc/>
    public string? Decrypt(string cipherText) => _provider.Decrypt(cipherText);
}
