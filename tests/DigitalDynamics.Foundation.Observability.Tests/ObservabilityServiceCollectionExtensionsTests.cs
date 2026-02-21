// =============================================================================
// Tests - ObservabilityServiceCollectionExtensions
// =============================================================================
// Vérifie que AddFoundationObservability enregistre correctement Serilog
// et OpenTelemetry (traces + métriques) dans le conteneur DI.
// =============================================================================

using DigitalDynamics.Foundation.Observability.Extensions;
using DigitalDynamics.Foundation.Observability.Options;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Xunit;

namespace DigitalDynamics.Foundation.Observability.Tests;

public sealed class ObservabilityServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFoundationObservability_RegistersObservabilityOptions()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder([]);
        builder.Configuration["Observability:ServiceName"] = "test-service";
        builder.Configuration["Observability:ServiceVersion"] = "1.2.3";

        // Act
        builder.AddFoundationObservability();

        using var sp = builder.Services.BuildServiceProvider();

        // Assert
        var options = sp.GetRequiredService<IOptions<ObservabilityOptions>>().Value;
        options.ServiceName.Should().Be("test-service");
        options.ServiceVersion.Should().Be("1.2.3");
    }

    [Fact]
    public void AddFoundationObservability_RegistersTracerProvider()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder([]);
        builder.Configuration["Observability:EnableTracing"] = "true";
        builder.Configuration["Observability:OtlpEndpoint"] = "http://localhost:4317";

        // Act
        builder.AddFoundationObservability();

        using var sp = builder.Services.BuildServiceProvider();

        // Assert
        var tracerProvider = sp.GetService<TracerProvider>();
        tracerProvider.Should().NotBeNull();
    }

    [Fact]
    public void AddFoundationObservability_RegistersMeterProvider()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder([]);
        builder.Configuration["Observability:EnableMetrics"] = "true";
        builder.Configuration["Observability:OtlpEndpoint"] = "http://localhost:4317";

        // Act
        builder.AddFoundationObservability();

        using var sp = builder.Services.BuildServiceProvider();

        // Assert
        var meterProvider = sp.GetService<MeterProvider>();
        meterProvider.Should().NotBeNull();
    }

    [Fact]
    public void AddFoundationObservability_WithDefaultConfig_UsesDefaultOptions()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder([]);

        // Act
        builder.AddFoundationObservability();

        using var sp = builder.Services.BuildServiceProvider();

        // Assert
        var options = sp.GetRequiredService<IOptions<ObservabilityOptions>>().Value;
        options.ServiceName.Should().Be("unknown-service");
        options.OtlpEndpoint.Should().Be("http://localhost:4317");
        options.EnableTracing.Should().BeTrue();
        options.EnableMetrics.Should().BeTrue();
    }

    [Fact]
    public void AddFoundationObservability_ReturnsBuilder_ForChaining()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder([]);

        // Act
        var result = builder.AddFoundationObservability();

        // Assert
        result.Should().BeSameAs(builder);
    }
}
