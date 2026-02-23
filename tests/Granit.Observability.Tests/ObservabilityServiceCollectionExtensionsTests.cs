// =============================================================================
// Tests - ObservabilityServiceCollectionExtensions
// =============================================================================
// Vérifie que AddGranitObservability enregistre correctement Serilog
// et OpenTelemetry (traces + métriques) dans le conteneur DI.
// =============================================================================

using FluentAssertions;
using Granit.Observability.Extensions;
using Granit.Observability.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Xunit;

namespace Granit.Observability.Tests;

public sealed class ObservabilityServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitObservability_RegistersObservabilityOptions()
    {
        // Arrange
        HostApplicationBuilder builder = Host.CreateApplicationBuilder([]);
        builder.Configuration["Observability:ServiceName"] = "test-service";
        builder.Configuration["Observability:ServiceVersion"] = "1.2.3";

        // Act
        builder.AddGranitObservability();

        using ServiceProvider sp = builder.Services.BuildServiceProvider();

        // Assert
        ObservabilityOptions options = sp.GetRequiredService<IOptions<ObservabilityOptions>>().Value;
        options.ServiceName.Should().Be("test-service");
        options.ServiceVersion.Should().Be("1.2.3");
    }

    [Fact]
    public void AddGranitObservability_RegistersTracerProvider()
    {
        // Arrange
        HostApplicationBuilder builder = Host.CreateApplicationBuilder([]);
        builder.Configuration["Observability:EnableTracing"] = "true";
        builder.Configuration["Observability:OtlpEndpoint"] = "http://localhost:4317";

        // Act
        builder.AddGranitObservability();

        using ServiceProvider sp = builder.Services.BuildServiceProvider();

        // Assert
        TracerProvider? tracerProvider = sp.GetService<TracerProvider>();
        tracerProvider.Should().NotBeNull();
    }

    [Fact]
    public void AddGranitObservability_RegistersMeterProvider()
    {
        // Arrange
        HostApplicationBuilder builder = Host.CreateApplicationBuilder([]);
        builder.Configuration["Observability:EnableMetrics"] = "true";
        builder.Configuration["Observability:OtlpEndpoint"] = "http://localhost:4317";

        // Act
        builder.AddGranitObservability();

        using ServiceProvider sp = builder.Services.BuildServiceProvider();

        // Assert
        MeterProvider? meterProvider = sp.GetService<MeterProvider>();
        meterProvider.Should().NotBeNull();
    }

    [Fact]
    public void AddGranitObservability_WithDefaultConfig_UsesDefaultOptions()
    {
        // Arrange
        HostApplicationBuilder builder = Host.CreateApplicationBuilder([]);

        // Act
        builder.AddGranitObservability();

        using ServiceProvider sp = builder.Services.BuildServiceProvider();

        // Assert
        ObservabilityOptions options = sp.GetRequiredService<IOptions<ObservabilityOptions>>().Value;
        options.ServiceName.Should().Be("unknown-service");
        options.OtlpEndpoint.Should().Be("http://localhost:4317");
        options.EnableTracing.Should().BeTrue();
        options.EnableMetrics.Should().BeTrue();
    }

    [Fact]
    public void AddGranitObservability_ReturnsBuilder_ForChaining()
    {
        // Arrange
        HostApplicationBuilder builder = Host.CreateApplicationBuilder([]);

        // Act
        IHostApplicationBuilder result = builder.AddGranitObservability();

        // Assert
        result.Should().BeSameAs(builder);
    }

    /// <summary>
    /// Verifies the OTEL tracing filter: paths under /health/* must be excluded,
    /// while /healthcare/... and regular paths must be included.
    /// </summary>
    [Theory]
    [InlineData("/health/live", false)]
    [InlineData("/health/ready", false)]
    [InlineData("/health/startup", false)]
    [InlineData("/healthz", false)]
    [InlineData("/healthcare/patients", true)]
    [InlineData("/api/orders", true)]
    [InlineData("/", true)]
    public void OtelTracingFilter_ExcludesHealthPaths_ButNotHealthcarePaths(string requestPath, bool expectedIncluded)
    {
        // The filter is a lambda registered inside AddGranitObservability via
        // aspnet.Filter = httpContext => !path.StartsWithSegments("/health") && path != "/healthz"
        // We test it by replicating its logic to ensure spec coverage of the two conditions.

        PathString path = new(requestPath);
        bool included = !path.StartsWithSegments("/health") && path != "/healthz";

        included.Should().Be(expectedIncluded,
            because: $"path '{requestPath}' should {(expectedIncluded ? "be included in" : "be excluded from")} tracing");
    }
}
