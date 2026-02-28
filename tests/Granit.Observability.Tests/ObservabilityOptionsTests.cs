// =============================================================================
// Tests - ObservabilityOptions
// =============================================================================
// Verifies that observability options have correct default values
// and that binding from configuration works correctly.
// =============================================================================

using Granit.Observability.Options;
using Microsoft.Extensions.Configuration;
using Shouldly;
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
        options.ServiceName.ShouldBe("unknown-service");
        options.ServiceVersion.ShouldBe("0.0.0");
        options.OtlpEndpoint.ShouldBe("http://localhost:4317");
        options.ServiceNamespace.ShouldBe("guava-health");
        options.Environment.ShouldBe("development");
        options.EnableTracing.ShouldBeTrue();
        options.EnableMetrics.ShouldBeTrue();
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
        options.ShouldNotBeNull();
        options!.ServiceName.ShouldBe("guava-backend");
        options.ServiceVersion.ShouldBe("1.0.0");
        options.OtlpEndpoint.ShouldBe("http://otel-collector:4317");
        options.Environment.ShouldBe("production");
        options.EnableTracing.ShouldBeTrue();
        options.EnableMetrics.ShouldBeFalse();
    }

    [Fact]
    public void SectionName_IsCorrect() => ObservabilityOptions.SectionName.ShouldBe("Observability");
}
