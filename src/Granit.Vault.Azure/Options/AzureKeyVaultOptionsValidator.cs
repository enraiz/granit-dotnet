using Microsoft.Extensions.Options;

namespace Granit.Vault.Azure.Options;

/// <summary>Validates <see cref="AzureKeyVaultOptions"/>.</summary>
internal sealed class AzureKeyVaultOptionsValidator : IValidateOptions<AzureKeyVaultOptions>
{
    private static readonly HashSet<string> s_supportedAlgorithms =
        ["RSA-OAEP", "RSA-OAEP-256", "RSA1_5"];

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, AzureKeyVaultOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.VaultUri))
        {
            return ValidateOptionsResult.Fail("AzureKeyVaultOptions.VaultUri is required.");
        }

        if (!Uri.TryCreate(options.VaultUri, UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            return ValidateOptionsResult.Fail("AzureKeyVaultOptions.VaultUri must be a valid absolute URI.");
        }

        if (string.IsNullOrWhiteSpace(options.EncryptionKeyName))
        {
            return ValidateOptionsResult.Fail("AzureKeyVaultOptions.EncryptionKeyName is required.");
        }

        if (!s_supportedAlgorithms.Contains(options.EncryptionAlgorithm))
        {
            return ValidateOptionsResult.Fail(
                $"AzureKeyVaultOptions.EncryptionAlgorithm '{options.EncryptionAlgorithm}' is not supported. " +
                $"Supported values: {string.Join(", ", s_supportedAlgorithms)}.");
        }

        if (options.RotationCheckIntervalMinutes < 1)
        {
            return ValidateOptionsResult.Fail("AzureKeyVaultOptions.RotationCheckIntervalMinutes must be at least 1.");
        }

        if (options.TimeoutSeconds < 1)
        {
            return ValidateOptionsResult.Fail("AzureKeyVaultOptions.TimeoutSeconds must be at least 1.");
        }

        return ValidateOptionsResult.Success;
    }
}
