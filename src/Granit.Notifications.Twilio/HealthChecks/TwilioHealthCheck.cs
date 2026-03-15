using System.Net;
using Granit.Notifications.Twilio.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Granit.Notifications.Twilio.HealthChecks;

/// <summary>
/// Health check that verifies Twilio API connectivity by calling
/// <c>GET /2010-04-01/Accounts/{AccountSid}.json</c>.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item>200 OK → <see cref="HealthCheckResult.Healthy"/></item>
///   <item>401/403 (credentials invalid) → <see cref="HealthCheckResult.Unhealthy"/></item>
///   <item>5xx (Twilio issue) → <see cref="HealthCheckResult.Degraded"/></item>
///   <item>Unreachable or timeout → <see cref="HealthCheckResult.Unhealthy"/></item>
/// </list>
/// The response never exposes credentials or account details.
/// </remarks>
internal sealed class TwilioHealthCheck(
    IHttpClientFactory httpClientFactory,
    IOptionsMonitor<TwilioOptions> options) : IHealthCheck
{
    private static readonly TimeSpan s_healthCheckTimeout = TimeSpan.FromSeconds(10);

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using HttpClient client = httpClientFactory.CreateClient("Twilio");
            TwilioOptions opts = options.CurrentValue;

            using HttpResponseMessage response = await client
                .GetAsync($"2010-04-01/Accounts/{opts.AccountSid}.json", cancellationToken)
                .WaitAsync(s_healthCheckTimeout, cancellationToken)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy();
            }

            return response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                ? HealthCheckResult.Unhealthy($"Twilio auth failed: {(int)response.StatusCode}")
                : HealthCheckResult.Degraded($"Twilio returned {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            // Sanitize: never expose credentials or base URL
            return HealthCheckResult.Unhealthy($"Twilio unreachable: {ex.GetType().Name}");
        }
    }
}
