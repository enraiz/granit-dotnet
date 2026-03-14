using Microsoft.Extensions.Options;

namespace Granit.Notifications.Sms.Sns.Options;

/// <summary>Validates <see cref="SnsSmsOptions"/>.</summary>
internal sealed class SnsSmsOptionsValidator : IValidateOptions<SnsSmsOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, SnsSmsOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Region))
        {
            return ValidateOptionsResult.Fail("SnsSmsOptions.Region is required.");
        }

        if (options.SmsType is not ("Transactional" or "Promotional"))
        {
            return ValidateOptionsResult.Fail("SnsSmsOptions.SmsType must be 'Transactional' or 'Promotional'.");
        }

        if (!string.IsNullOrEmpty(options.AccessKeyId) && string.IsNullOrEmpty(options.SecretAccessKey))
        {
            return ValidateOptionsResult.Fail("SnsSmsOptions.SecretAccessKey is required when AccessKeyId is set.");
        }

        if (!string.IsNullOrEmpty(options.SecretAccessKey) && string.IsNullOrEmpty(options.AccessKeyId))
        {
            return ValidateOptionsResult.Fail("SnsSmsOptions.AccessKeyId is required when SecretAccessKey is set.");
        }

        if (options.TimeoutSeconds < 1)
        {
            return ValidateOptionsResult.Fail("SnsSmsOptions.TimeoutSeconds must be at least 1.");
        }

        return ValidateOptionsResult.Success;
    }
}
