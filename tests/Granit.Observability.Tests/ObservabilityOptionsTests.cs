// =============================================================================
// Tests - ObservabilityOptions
// =============================================================================
// Verifies that observability options have correct default values
// and that binding from configuration works correctly.
// =============================================================================

using FluentAssertions;
using Granit.Observability.Options;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Granit.Observability.Tests;

public sealed class ObservabilityOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new ObservabilityOptions();

        // Assert
        options.ServiceName.Should().Be("unknown-service");
        options.ServiceVersion.Should().Be("0.0.0");
        options.OtlpEndpoint.Should().Be("http://localhost:4317");
        options.ServiceNamespace.Should().Be("guava-health");
        options.Environment.Should().Be("development");
        options.EnableTracing.Should().BeTrue();
        options.EnableMetrics.Should().BeTrue();
    }

    [Fact]
    public void Binding_FromConfiguration_Works()
    {
        // Arrange
        IConfigurationRoot config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{ObservabilityOptions.SectionName}:ServiceName"] = "guava-backend",
                [$"{ObservabilityOptions.SectionName}:ServiceVersion"] = "1.0.0",
                [$"{ObservabilityOptions.SectionName}:OtlpEndpoint"] = "http://otel-collector:4317",
                [$"{ObservabilityOptions.SectionName}:Environment"] = "production",
                [$"{ObservabilityOptions.SectionName}:EnableTracing"] = "true",
                [$"{ObservabilityOptions.SectionName}:EnableMetrics"] = "false"
            })
            .Build();

        // Act
        ObservabilityOptions? options = config.GetSection(ObservabilityOptions.SectionName)
            .Get<ObservabilityOptions>();

        // Assert
        options.Should().NotBeNull();
        options!.ServiceName.Should().Be("guava-backend");
        options.ServiceVersion.Should().Be("1.0.0");
        options.OtlpEndpoint.Should().Be("http://otel-collector:4317");
        options.Environment.Should().Be("production");
        options.EnableTracing.Should().BeTrue();
        options.EnableMetrics.Should().BeFalse();
    }

    [Fact]
    public void SectionName_IsCorrect() => ObservabilityOptions.SectionName.Should().Be("Observability");
}
