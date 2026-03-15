using System.Net;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Granit.Notifications.Email.Scaleway.HealthChecks;

/// <summary>
/// Health check that verifies Scaleway Transactional Email API connectivity by calling the
/// <c>GET emails?page_size=1</c> endpoint.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item>200 OK → <see cref="HealthCheckResult.Healthy"/></item>
///   <item>401/403 (secret key invalid) → <see cref="HealthCheckResult.Unhealthy"/></item>
///   <item>5xx (Scaleway issue) → <see cref="HealthCheckResult.Degraded"/></item>
///   <item>Unreachable or timeout → <see cref="HealthCheckResult.Unhealthy"/></item>
/// </list>
/// The response never exposes secret keys or account details.
/// </remarks>
internal sealed class ScalewayEmailHealthCheck(IHttpClientFactory httpClientFactory) : IHealthCheck
{
    private static readonly TimeSpan s_healthCheckTimeout = TimeSpan.FromSeconds(10);

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using HttpClient client = httpClientFactory.CreateClient("Scaleway");

            using HttpResponseMessage response = await client
                .GetAsync("emails?page_size=1", cancellationToken)
                .WaitAsync(s_healthCheckTimeout, cancellationToken)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy();
            }

            return response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                ? HealthCheckResult.Unhealthy($"Scaleway auth failed: {(int)response.StatusCode}")
                : HealthCheckResult.Degraded($"Scaleway returned {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            // Sanitize: never expose secret keys or base URL
            return HealthCheckResult.Unhealthy($"Scaleway unreachable: {ex.GetType().Name}");
        }
    }
}
