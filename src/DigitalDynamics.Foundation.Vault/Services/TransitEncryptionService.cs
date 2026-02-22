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
using VaultSharp.V1.Commons;
using VaultSharp.V1.SecretsEngines.Transit;

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
        string base64Plaintext = Convert.ToBase64String(Encoding.UTF8.GetBytes(plaintext));

        Secret<EncryptionResponse> result = await _vaultClient.V1.Secrets.Transit.EncryptAsync(
            keyName,
            new EncryptRequestOptions
            {
                Base64EncodedPlainText = base64Plaintext
            },
            mountPoint: _options.TransitMountPoint);

<<<<<<< HEAD
        _logger.LogDebug("Data encrypted with Transit key {KeyName}", keyName);
=======
        LogDataEncrypted(_logger, keyName);
>>>>>>> feature/settings-module
        return result.Data.CipherText;
    }

    public async Task<string> DecryptAsync(
        string keyName,
        string ciphertext,
        CancellationToken cancellationToken = default)
    {
        Secret<DecryptionResponse> result = await _vaultClient.V1.Secrets.Transit.DecryptAsync(
            keyName,
            new DecryptRequestOptions
            {
                CipherText = ciphertext
            },
            mountPoint: _options.TransitMountPoint);

<<<<<<< HEAD
        var bytes = Convert.FromBase64String(result.Data.Base64EncodedPlainText);
        _logger.LogDebug("Data decrypted with Transit key {KeyName}", keyName);
=======
        byte[] bytes = Convert.FromBase64String(result.Data.Base64EncodedPlainText);
        LogDataDecrypted(_logger, keyName);
>>>>>>> feature/settings-module
        return Encoding.UTF8.GetString(bytes);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Data encrypted with Transit key {KeyName}")]
    private static partial void LogDataEncrypted(ILogger logger, string keyName);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Data decrypted with Transit key {KeyName}")]
    private static partial void LogDataDecrypted(ILogger logger, string keyName);
}
