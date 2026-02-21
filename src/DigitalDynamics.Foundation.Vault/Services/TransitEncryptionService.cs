// =============================================================================
// TransitEncryptionService - Chiffrement/déchiffrement via Vault Transit
// =============================================================================
// Implémente ITransitEncryptionService pour le chiffrement des données FHIR
// via l'engine Transit de Vault (clé AES-256-GCM96).
//
// Conformité HDS : les données de santé sont chiffrées au repos via Vault.
// Vault gère les clés et leur rotation — aucune clé n'est stockée dans l'app.
// =============================================================================

using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DigitalDynamics.Foundation.Vault.Options;
using VaultSharp;

namespace DigitalDynamics.Foundation.Vault.Services;

/// <summary>
/// Implémentation de <see cref="ITransitEncryptionService"/> via Vault Transit Engine.
/// </summary>
public sealed class TransitEncryptionService : ITransitEncryptionService
{
    private readonly IVaultClient _vaultClient;
    private readonly VaultOptions _options;
    private readonly ILogger<TransitEncryptionService> _logger;

    public TransitEncryptionService(
        IVaultClient vaultClient,
        IOptions<VaultOptions> options,
        ILogger<TransitEncryptionService> logger)
    {
        _vaultClient = vaultClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> EncryptAsync(
        string keyName,
        string plaintext,
        CancellationToken cancellationToken = default)
    {
        var base64Plaintext = Convert.ToBase64String(Encoding.UTF8.GetBytes(plaintext));

        var result = await _vaultClient.V1.Secrets.Transit.EncryptAsync(
            keyName,
            new VaultSharp.V1.SecretsEngines.Transit.EncryptRequestOptions
            {
                Base64EncodedPlainText = base64Plaintext
            },
            mountPoint: _options.TransitMountPoint);

        _logger.LogDebug("Données chiffrées avec la clé Transit {KeyName}", keyName);
        return result.Data.CipherText;
    }

    public async Task<string> DecryptAsync(
        string keyName,
        string ciphertext,
        CancellationToken cancellationToken = default)
    {
        var result = await _vaultClient.V1.Secrets.Transit.DecryptAsync(
            keyName,
            new VaultSharp.V1.SecretsEngines.Transit.DecryptRequestOptions
            {
                CipherText = ciphertext
            },
            mountPoint: _options.TransitMountPoint);

        var bytes = Convert.FromBase64String(result.Data.Base64EncodedPlainText);
        _logger.LogDebug("Données déchiffrées avec la clé Transit {KeyName}", keyName);
        return Encoding.UTF8.GetString(bytes);
    }
}
