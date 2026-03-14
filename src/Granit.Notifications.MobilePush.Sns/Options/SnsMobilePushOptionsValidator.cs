using Microsoft.Extensions.Options;

namespace Granit.Notifications.MobilePush.Sns.Options;

/// <summary>Validates <see cref="SnsMobilePushOptions"/>.</summary>
internal sealed class SnsMobilePushOptionsValidator : IValidateOptions<SnsMobilePushOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, SnsMobilePushOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Region))
        {
            return ValidateOptionsResult.Fail("SnsMobilePushOptions.Region is required.");
        }

        if (string.IsNullOrWhiteSpace(options.PlatformApplicationArn))
        {
            return ValidateOptionsResult.Fail("SnsMobilePushOptions.PlatformApplicationArn is required.");
        }

        if (!string.IsNullOrEmpty(options.AccessKeyId) && string.IsNullOrEmpty(options.SecretAccessKey))
        {
            return ValidateOptionsResult.Fail("SnsMobilePushOptions.SecretAccessKey is required when AccessKeyId is set.");
        }

        if (!string.IsNullOrEmpty(options.SecretAccessKey) && string.IsNullOrEmpty(options.AccessKeyId))
        {
            return ValidateOptionsResult.Fail("SnsMobilePushOptions.AccessKeyId is required when SecretAccessKey is set.");
        }

        if (options.TimeoutSeconds < 1)
        {
            return ValidateOptionsResult.Fail("SnsMobilePushOptions.TimeoutSeconds must be at least 1.");
        }

        return ValidateOptionsResult.Success;
    }
}
