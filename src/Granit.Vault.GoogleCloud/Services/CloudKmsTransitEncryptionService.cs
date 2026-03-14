using System.Text;
using Google.Cloud.Kms.V1;
using Google.Protobuf;
using Granit.Vault.GoogleCloud.Diagnostics;
using Granit.Vault.GoogleCloud.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Granit.Vault.GoogleCloud.Services;

/// <summary>
/// Transit encryption service using Google Cloud KMS symmetric encryption.
/// </summary>
internal sealed partial class CloudKmsTransitEncryptionService(
    KeyManagementServiceClient kmsClient,
    IOptions<GoogleCloudVaultOptions> options,
    ILogger<CloudKmsTransitEncryptionService> logger) : ITransitEncryptionService
{
    private readonly CryptoKeyName _keyName = new(
        options.Value.ProjectId,
        options.Value.Location,
        options.Value.KeyRing,
        options.Value.CryptoKey);

    /// <inheritdoc />
    public async Task<string> EncryptAsync(
        string keyName,
        string plaintext,
        CancellationToken cancellationToken = default)
    {
        using System.Diagnostics.Activity? activity = VaultGoogleCloudActivitySource.Source.StartActivity(
            VaultGoogleCloudActivitySource.Operations.KmsEncrypt);
        activity?.SetTag(VaultGoogleCloudActivitySource.Tags.KeyName, keyName);

        byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);

        EncryptResponse response = await kmsClient.EncryptAsync(
            _keyName,
            ByteString.CopyFrom(plaintextBytes),
            cancellationToken).ConfigureAwait(false);

        string ciphertext = Convert.ToBase64String(response.Ciphertext.ToByteArray());

        LogEncryptSuccess(keyName);
        return ciphertext;
    }

    /// <inheritdoc />
    public async Task<string> DecryptAsync(
        string keyName,
        string ciphertext,
        CancellationToken cancellationToken = default)
    {
        using System.Diagnostics.Activity? activity = VaultGoogleCloudActivitySource.Source.StartActivity(
            VaultGoogleCloudActivitySource.Operations.KmsDecrypt);
        activity?.SetTag(VaultGoogleCloudActivitySource.Tags.KeyName, keyName);

        byte[] ciphertextBytes = Convert.FromBase64String(ciphertext);

        DecryptResponse response = await kmsClient.DecryptAsync(
            _keyName,
            ByteString.CopyFrom(ciphertextBytes),
            cancellationToken).ConfigureAwait(false);

        string result = Encoding.UTF8.GetString(response.Plaintext.ToByteArray());

        LogDecryptSuccess(keyName);
        return result;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cloud KMS encrypt succeeded for key {KeyName}")]
    private partial void LogEncryptSuccess(string keyName);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cloud KMS decrypt succeeded for key {KeyName}")]
    private partial void LogDecryptSuccess(string keyName);
}
