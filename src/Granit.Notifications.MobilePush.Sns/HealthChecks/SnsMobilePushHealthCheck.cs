using Granit.Notifications.MobilePush.Sns.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Granit.Notifications.MobilePush.Sns.HealthChecks;

/// <summary>
/// Health check that verifies SNS mobile push configuration is valid.
/// </summary>
internal sealed class SnsMobilePushHealthCheck(
    IOptions<SnsMobilePushOptions> options) : IHealthCheck
{
    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            SnsMobilePushOptions opts = options.Value;

            if (string.IsNullOrWhiteSpace(opts.Region))
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("SNS mobile push region is not configured."));
            }

            if (string.IsNullOrWhiteSpace(opts.PlatformApplicationArn))
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("SNS PlatformApplicationArn is not configured."));
            }

            return Task.FromResult(HealthCheckResult.Healthy());
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy($"SNS mobile push health check failed: {ex.GetType().Name}"));
        }
    }
}
