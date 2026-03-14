using Microsoft.Extensions.Options;

namespace Granit.BlobStorage.Proxy.Options;

/// <summary>
/// Validates <see cref="ProxyBlobOptions"/> at startup (fail-fast on misconfiguration).
/// </summary>
internal sealed class ProxyBlobOptionsValidator : IValidateOptions<ProxyBlobOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, ProxyBlobOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            return ValidateOptionsResult.Fail(
                $"{nameof(options.BaseUrl)} must be non-empty. " +
                "Set it to the externally reachable application URL (e.g. https://api.example.com).");
        }

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _))
        {
            return ValidateOptionsResult.Fail(
                $"{nameof(options.BaseUrl)} must be a valid absolute URI.");
        }

        if (options.MaxUploadBytes <= 0)
        {
            return ValidateOptionsResult.Fail(
                $"{nameof(options.MaxUploadBytes)} must be a positive value.");
        }

        if (string.IsNullOrWhiteSpace(options.RoutePrefix) || !options.RoutePrefix.StartsWith('/'))
        {
            return ValidateOptionsResult.Fail(
                $"{nameof(options.RoutePrefix)} must start with '/'.");
        }

        return ValidateOptionsResult.Success;
    }
}
