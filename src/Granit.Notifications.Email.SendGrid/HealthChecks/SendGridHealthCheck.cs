using System.Net;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Granit.Notifications.Email.SendGrid.HealthChecks;

/// <summary>
/// Health check that verifies SendGrid API connectivity by calling the
/// <c>GET scopes</c> endpoint.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item>200 OK → <see cref="HealthCheckResult.Healthy"/></item>
///   <item>401/403 (API key invalid) → <see cref="HealthCheckResult.Unhealthy"/></item>
///   <item>5xx (SendGrid issue) → <see cref="HealthCheckResult.Degraded"/></item>
///   <item>Unreachable or timeout → <see cref="HealthCheckResult.Unhealthy"/></item>
/// </list>
/// The response never exposes API keys or account details.
/// </remarks>
internal sealed class SendGridHealthCheck(IHttpClientFactory httpClientFactory) : IHealthCheck
{
    private static readonly TimeSpan s_healthCheckTimeout = TimeSpan.FromSeconds(10);

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using HttpClient client = httpClientFactory.CreateClient("SendGrid");

            using HttpResponseMessage response = await client
                .GetAsync("scopes", cancellationToken)
                .WaitAsync(s_healthCheckTimeout, cancellationToken)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy();
            }

            return response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                ? HealthCheckResult.Unhealthy($"SendGrid auth failed: {(int)response.StatusCode}")
                : HealthCheckResult.Degraded($"SendGrid returned {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            // Sanitize: never expose API keys or base URL
            return HealthCheckResult.Unhealthy($"SendGrid unreachable: {ex.GetType().Name}");
        }
    }
}
