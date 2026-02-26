using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Granit.Diagnostics.ResponseWriters;

/// <summary>
/// Writes a structured JSON health check response for observability tooling (Grafana, Loki).
/// Kubernetes only reads the HTTP status code; the JSON payload is for operations teams.
/// </summary>
/// <remarks>
/// The response never contains stack traces, connection strings, tokens, or any PII,
/// in compliance with HDS and RGPD constraints.
/// </remarks>
public static class GranitHealthCheckWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Writes the <see cref="HealthReport"/> as a JSON response.
    /// </summary>
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        HealthResponse response = new(
            Status: report.Status.ToString(),
            Duration: Math.Round(report.TotalDuration.TotalMilliseconds, 1),
            Checks: [.. report.Entries.Select(e => new CheckEntry(
                Name: e.Key,
                Status: e.Value.Status.ToString(),
                Duration: Math.Round(e.Value.Duration.TotalMilliseconds, 1),
                Description: e.Value.Description,
                Tags: [.. e.Value.Tags]))]);

        return context.Response.WriteAsync(
            JsonSerializer.Serialize(response, SerializerOptions),
            context.RequestAborted);
    }

    private sealed record HealthResponse(
        string Status,
        double Duration,
        IReadOnlyList<CheckEntry> Checks);

    private sealed record CheckEntry(
        string Name,
        string Status,
        double Duration,
        string? Description,
        IReadOnlyList<string>? Tags);
}
