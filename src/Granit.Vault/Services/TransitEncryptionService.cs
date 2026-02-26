using System.Text;
using Granit.Vault.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VaultSharp;
using VaultSharp.V1.Commons;
using VaultSharp.V1.SecretsEngines.Transit;

namespace Granit.Vault.Services;

/// <summary>
/// Implementation of <see cref="ITransitEncryptionService"/> via Vault Transit Engine.
/// </summary>
public sealed partial class TransitEncryptionService(
    IVaultClient vaultClient,
    IOptions<VaultOptions> options,
    ILogger<TransitEncryptionService> logger) : ITransitEncryptionService
{
    private readonly IVaultClient _vaultClient = vaultClient;
    private readonly VaultOptions _options = options.Value;
    private readonly ILogger<TransitEncryptionService> _logger = logger;

    public async Task<string> EncryptAsync(
        string keyName,
        string plaintext,
        CancellationToken cancellationToken = default)
    {
        string base64Plaintext = Convert.ToBase64String(Encoding.UTF8.GetBytes(plaintext));

        // VaultSharp API does not expose cancellation — WaitAsync provides a defensive timeout.
        Secret<EncryptionResponse> result = await _vaultClient.V1.Secrets.Transit.EncryptAsync(
            keyName,
            new EncryptRequestOptions
            {
                Base64EncodedPlainText = base64Plaintext
            },
            mountPoint: _options.TransitMountPoint).WaitAsync(cancellationToken);

        LogEncrypted(_logger, keyName);
        return result.Data.CipherText;
    }

    public async Task<string> DecryptAsync(
        string keyName,
        string ciphertext,
        CancellationToken cancellationToken = default)
    {
        // VaultSharp API does not expose cancellation — WaitAsync provides a defensive timeout.
        Secret<DecryptionResponse> result = await _vaultClient.V1.Secrets.Transit.DecryptAsync(
            keyName,
            new DecryptRequestOptions
            {
                CipherText = ciphertext
            },
            mountPoint: _options.TransitMountPoint).WaitAsync(cancellationToken);

        byte[] bytes = Convert.FromBase64String(result.Data.Base64EncodedPlainText);
        LogDecrypted(_logger, keyName);
        return Encoding.UTF8.GetString(bytes);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Data encrypted with Transit key {KeyName}")]
    private static partial void LogEncrypted(ILogger logger, string keyName);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Data decrypted with Transit key {KeyName}")]
    private static partial void LogDecrypted(ILogger logger, string keyName);
}
