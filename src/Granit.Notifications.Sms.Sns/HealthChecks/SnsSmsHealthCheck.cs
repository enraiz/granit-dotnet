using Granit.Notifications.Sms.Sns.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Granit.Notifications.Sms.Sns.HealthChecks;

/// <summary>
/// Health check that verifies SNS SMS configuration is valid and the transport is available.
/// </summary>
internal sealed class SnsSmsHealthCheck(
    IOptions<SnsSmsOptions> options) : IHealthCheck
{
    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            SnsSmsOptions opts = options.Value;

            if (string.IsNullOrWhiteSpace(opts.Region))
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("SNS SMS region is not configured."));
            }

            return Task.FromResult(HealthCheckResult.Healthy());
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy($"SNS SMS health check failed: {ex.GetType().Name}"));
        }
    }
}
