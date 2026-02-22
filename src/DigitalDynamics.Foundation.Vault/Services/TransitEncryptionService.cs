// =============================================================================
// TransitEncryptionService - Encryption/decryption via Vault Transit
// =============================================================================
// Implements ITransitEncryptionService for encrypting FHIR data
// via the Vault Transit engine (AES-256-GCM96 key).
//
// HDS compliance: health data is encrypted at rest via Vault.
// Vault manages the keys and their rotation — no key is stored in the app.
// =============================================================================

using System.Text;
using DigitalDynamics.Foundation.Vault.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VaultSharp;

namespace DigitalDynamics.Foundation.Vault.Services;

/// <summary>
/// Implementation of <see cref="ITransitEncryptionService"/> via Vault Transit Engine.
/// </summary>
public sealed partial class TransitEncryptionService : ITransitEncryptionService
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

        LogEncrypted(_logger, keyName);
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
        LogDecrypted(_logger, keyName);
        return Encoding.UTF8.GetString(bytes);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Data encrypted with Transit key {KeyName}")]
    private static partial void LogEncrypted(ILogger logger, string keyName);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Data decrypted with Transit key {KeyName}")]
    private static partial void LogDecrypted(ILogger logger, string keyName);
}
