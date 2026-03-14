using Microsoft.Extensions.Options;

namespace Granit.Vault.Aws.Options;

/// <summary>Validates <see cref="AwsVaultOptions"/>.</summary>
internal sealed class AwsVaultOptionsValidator : IValidateOptions<AwsVaultOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, AwsVaultOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Region))
        {
            return ValidateOptionsResult.Fail("AwsVaultOptions.Region is required.");
        }

        if (string.IsNullOrWhiteSpace(options.KmsKeyId))
        {
            return ValidateOptionsResult.Fail("AwsVaultOptions.KmsKeyId is required.");
        }

        if (options.RotationCheckIntervalMinutes < 1)
        {
            return ValidateOptionsResult.Fail("AwsVaultOptions.RotationCheckIntervalMinutes must be at least 1.");
        }

        if (options.TimeoutSeconds < 1)
        {
            return ValidateOptionsResult.Fail("AwsVaultOptions.TimeoutSeconds must be at least 1.");
        }

        if (options.AccessKeyId is not null && options.SecretAccessKey is null)
        {
            return ValidateOptionsResult.Fail(
                "AwsVaultOptions.SecretAccessKey is required when AccessKeyId is provided.");
        }

        if (options.SecretAccessKey is not null && options.AccessKeyId is null)
        {
            return ValidateOptionsResult.Fail(
                "AwsVaultOptions.AccessKeyId is required when SecretAccessKey is provided.");
        }

        return ValidateOptionsResult.Success;
    }
}
