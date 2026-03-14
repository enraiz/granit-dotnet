using Microsoft.Extensions.Options;

namespace Granit.Notifications.Email.Ses.Options;

/// <summary>Validates <see cref="SesOptions"/>.</summary>
internal sealed class SesOptionsValidator : IValidateOptions<SesOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, SesOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Region))
        {
            return ValidateOptionsResult.Fail("SesOptions.Region is required.");
        }

        if (options.TimeoutSeconds < 1)
        {
            return ValidateOptionsResult.Fail("SesOptions.TimeoutSeconds must be at least 1.");
        }

        if (options.AccessKeyId is not null && options.SecretAccessKey is null)
        {
            return ValidateOptionsResult.Fail(
                "SesOptions.SecretAccessKey is required when AccessKeyId is provided.");
        }

        if (options.SecretAccessKey is not null && options.AccessKeyId is null)
        {
            return ValidateOptionsResult.Fail(
                "SesOptions.AccessKeyId is required when SecretAccessKey is provided.");
        }

        return ValidateOptionsResult.Success;
    }
}
