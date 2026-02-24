using Microsoft.Extensions.Options;

namespace Granit.Webhooks;

/// <summary>
/// Configuration options for the Granit.Webhooks module.
/// </summary>
/// <remarks>
/// Bound from the <c>"Webhooks"</c> section of <c>appsettings.json</c>.
/// </remarks>
public sealed class WebhooksOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Webhooks";

    /// <summary>
    /// HTTP request timeout for webhook delivery, in seconds.
    /// Must be between 5 and 120. Default: 10.
    /// </summary>
    public int HttpTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Maximum number of <see cref="Messages.SendWebhookCommand"/> processed in parallel
    /// on the <c>webhook-delivery</c> local queue.
    /// Must be between 1 and 100. Default: 20.
    /// </summary>
    public int MaxParallelDeliveries { get; set; } = 20;
}

/// <summary>
/// Validates <see cref="WebhooksOptions"/> at startup.
/// </summary>
internal sealed class WebhooksOptionsValidator : IValidateOptions<WebhooksOptions>
{
    public ValidateOptionsResult Validate(string? name, WebhooksOptions options)
    {
        List<string> errors = [];

        if (options.HttpTimeoutSeconds < 5 || options.HttpTimeoutSeconds > 120)
        {
            errors.Add($"{nameof(WebhooksOptions.HttpTimeoutSeconds)} must be between 5 and 120 (got {options.HttpTimeoutSeconds}).");
        }

        if (options.MaxParallelDeliveries < 1 || options.MaxParallelDeliveries > 100)
        {
            errors.Add($"{nameof(WebhooksOptions.MaxParallelDeliveries)} must be between 1 and 100 (got {options.MaxParallelDeliveries}).");
        }

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }
}
