using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Granit.Notifications.Zulip.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Granit.Notifications.Zulip.HealthChecks;

/// <summary>
/// Health check that verifies Zulip Bot API connectivity by calling
/// <c>GET /api/v1/users/me</c> with Basic authentication.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item>200 OK → <see cref="HealthCheckResult.Healthy"/></item>
///   <item>401/403 (credentials invalid) → <see cref="HealthCheckResult.Unhealthy"/></item>
///   <item>5xx (Zulip server issue) → <see cref="HealthCheckResult.Degraded"/></item>
///   <item>Unreachable or timeout → <see cref="HealthCheckResult.Unhealthy"/></item>
/// </list>
/// The response never exposes API keys, bot emails, or server URLs.
/// </remarks>
internal sealed class ZulipHealthCheck(
    IHttpClientFactory httpClientFactory,
    IOptions<ZulipBotOptions> options) : IHealthCheck
{
    private static readonly TimeSpan s_healthCheckTimeout = TimeSpan.FromSeconds(10);

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ZulipBotOptions opts = options.Value;
            using HttpClient client = httpClientFactory.CreateClient("ZulipBot");

            string credentials = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{opts.BotEmail}:{opts.ApiKey}"));
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", credentials);

            using HttpResponseMessage response = await client
                .GetAsync("api/v1/users/me", cancellationToken)
                .WaitAsync(s_healthCheckTimeout, cancellationToken)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy();
            }

            return response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                ? HealthCheckResult.Unhealthy($"Zulip auth failed: {(int)response.StatusCode}")
                : HealthCheckResult.Degraded($"Zulip returned {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            // Sanitize: never expose server URL, bot email, or API key
            return HealthCheckResult.Unhealthy($"Zulip unreachable: {ex.GetType().Name}");
        }
    }
}
