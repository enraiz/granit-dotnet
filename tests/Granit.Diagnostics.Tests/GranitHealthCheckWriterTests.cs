using System.Text.Json;
using FluentAssertions;
using Granit.Diagnostics.ResponseWriters;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace Granit.Diagnostics.Tests;

public sealed class GranitHealthCheckWriterTests
{
    [Fact]
    public async Task WriteAsync_SetsContentType_ToApplicationJson()
    {
        DefaultHttpContext httpContext = new();
        httpContext.Response.Body = new MemoryStream();
        HealthReport report = BuildReport(HealthStatus.Healthy);

        await GranitHealthCheckWriter.WriteAsync(httpContext, report);

        httpContext.Response.ContentType.Should().Be("application/json; charset=utf-8");
    }

    [Fact]
    public async Task WriteAsync_WritesStatus_AsString()
    {
        DefaultHttpContext httpContext = new();
        httpContext.Response.Body = new MemoryStream();
        HealthReport report = BuildReport(HealthStatus.Unhealthy);

        await GranitHealthCheckWriter.WriteAsync(httpContext, report);

        JsonDocument json = ParseResponse(httpContext);
        json.RootElement.GetProperty("status").GetString().Should().Be("Unhealthy");
    }

    [Fact]
    public async Task WriteAsync_WritesChecks_WithNameAndStatus()
    {
        DefaultHttpContext httpContext = new();
        httpContext.Response.Body = new MemoryStream();

        Dictionary<string, HealthReportEntry> entries = new()
        {
            ["efcore"] = new HealthReportEntry(HealthStatus.Healthy, null, TimeSpan.FromMilliseconds(8), null, null, ["readiness"]),
            ["vault"] = new HealthReportEntry(HealthStatus.Degraded, "Vault standby", TimeSpan.FromMilliseconds(4), null, null, ["readiness"])
        };
        HealthReport report = new(entries, TimeSpan.FromMilliseconds(12));

        await GranitHealthCheckWriter.WriteAsync(httpContext, report);

        JsonDocument json = ParseResponse(httpContext);
        JsonElement checks = json.RootElement.GetProperty("checks");
        checks.GetArrayLength().Should().Be(2);

        JsonElement efcore = checks.EnumerateArray().First(e => e.GetProperty("name").GetString() == "efcore");
        efcore.GetProperty("status").GetString().Should().Be("Healthy");

        JsonElement vault = checks.EnumerateArray().First(e => e.GetProperty("name").GetString() == "vault");
        vault.GetProperty("status").GetString().Should().Be("Degraded");
        vault.GetProperty("description").GetString().Should().Be("Vault standby");
    }

    [Fact]
    public async Task WriteAsync_LivenessResponse_HasEmptyChecks()
    {
        DefaultHttpContext httpContext = new();
        httpContext.Response.Body = new MemoryStream();
        HealthReport report = new(new Dictionary<string, HealthReportEntry>(), TimeSpan.Zero);

        await GranitHealthCheckWriter.WriteAsync(httpContext, report);

        JsonDocument json = ParseResponse(httpContext);
        json.RootElement.GetProperty("checks").GetArrayLength().Should().Be(0);
    }

    private static HealthReport BuildReport(HealthStatus status) =>
        new(new Dictionary<string, HealthReportEntry>(), status, TimeSpan.FromMilliseconds(5));

    private static JsonDocument ParseResponse(DefaultHttpContext httpContext)
    {
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        return JsonDocument.Parse(httpContext.Response.Body);
    }
}
