using Microsoft.Extensions.Options;

namespace Granit.Vault.GoogleCloud.Options;

/// <summary>Validates <see cref="GoogleCloudVaultOptions"/>.</summary>
internal sealed class GoogleCloudVaultOptionsValidator : IValidateOptions<GoogleCloudVaultOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, GoogleCloudVaultOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ProjectId))
        {
            return ValidateOptionsResult.Fail("GoogleCloudVaultOptions.ProjectId is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Location))
        {
            return ValidateOptionsResult.Fail("GoogleCloudVaultOptions.Location is required.");
        }

        if (string.IsNullOrWhiteSpace(options.KeyRing))
        {
            return ValidateOptionsResult.Fail("GoogleCloudVaultOptions.KeyRing is required.");
        }

        if (string.IsNullOrWhiteSpace(options.CryptoKey))
        {
            return ValidateOptionsResult.Fail("GoogleCloudVaultOptions.CryptoKey is required.");
        }

        if (options.RotationCheckIntervalMinutes < 1)
        {
            return ValidateOptionsResult.Fail("GoogleCloudVaultOptions.RotationCheckIntervalMinutes must be at least 1.");
        }

        if (options.TimeoutSeconds < 1)
        {
            return ValidateOptionsResult.Fail("GoogleCloudVaultOptions.TimeoutSeconds must be at least 1.");
        }

        return ValidateOptionsResult.Success;
    }
}
