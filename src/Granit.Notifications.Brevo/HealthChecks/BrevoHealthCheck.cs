using System.Net;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Granit.Notifications.Brevo.HealthChecks;

/// <summary>
/// Health check that verifies Brevo API connectivity by calling the
/// <c>GET /account</c> endpoint.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item>200 OK → <see cref="HealthCheckResult.Healthy"/></item>
///   <item>401/403 (API key invalid) → <see cref="HealthCheckResult.Unhealthy"/></item>
///   <item>5xx (Brevo issue) → <see cref="HealthCheckResult.Degraded"/></item>
///   <item>Unreachable or timeout → <see cref="HealthCheckResult.Unhealthy"/></item>
/// </list>
/// The response never exposes API keys or account details.
/// </remarks>
internal sealed class BrevoHealthCheck(IHttpClientFactory httpClientFactory) : IHealthCheck
{
    private static readonly TimeSpan s_healthCheckTimeout = TimeSpan.FromSeconds(10);

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using HttpClient client = httpClientFactory.CreateClient("Brevo");

            using HttpResponseMessage response = await client
                .GetAsync("account", cancellationToken)
                .WaitAsync(s_healthCheckTimeout, cancellationToken)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy();
            }

            return response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                ? HealthCheckResult.Unhealthy($"Brevo auth failed: {(int)response.StatusCode}")
                : HealthCheckResult.Degraded($"Brevo returned {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            // Sanitize: never expose API keys or base URL
            return HealthCheckResult.Unhealthy($"Brevo unreachable: {ex.GetType().Name}");
        }
    }
}
