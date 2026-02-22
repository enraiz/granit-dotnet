// =============================================================================
// Tests - DiagnosticsServiceCollectionExtensions
// =============================================================================
// Vérifie les deux branches de AddFoundationDiagnostics :
//   - Sans configure → enregistrement HealthChecks sans options custom
//   - Avec configure != null → options personnalisées enregistrées
// =============================================================================

using DigitalDynamics.Foundation.Diagnostics.Extensions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Xunit;
using HealthCheckService = Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService;

namespace DigitalDynamics.Foundation.Diagnostics.Tests;

public sealed class DiagnosticsServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFoundationDiagnostics_WithoutConfigure_RegistersHealthChecks()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddFoundationDiagnostics();

        // Assert — AddHealthChecks was called: HealthCheckService is registered
        ServiceDescriptor? descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(HealthCheckService));
        descriptor.Should().NotBeNull("AddHealthChecks must register HealthCheckService");
    }

    [Fact]
    public void AddFoundationDiagnostics_WithConfigure_AppliesCustomOptions()
    {
        // Arrange
        ServiceCollection services = new();

        // Act — pass a configure action (the non-null branch)
        services.AddFoundationDiagnostics(opts =>
        {
            opts.LivenessPath = "/ping";
            opts.DefaultCacheDuration = TimeSpan.FromSeconds(30);
        });

        // Assert — DiagnosticsOptions reflects the customization
        using ServiceProvider sp = services.BuildServiceProvider();
        DiagnosticsOptions diagnosticsOptions = sp.GetRequiredService<IOptions<DiagnosticsOptions>>().Value;
        diagnosticsOptions.LivenessPath.Should().Be("/ping");
        diagnosticsOptions.DefaultCacheDuration.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void AddFoundationDiagnostics_ReturnsServices_ForChaining()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        IServiceCollection returned = services.AddFoundationDiagnostics();

        // Assert
        returned.Should().BeSameAs(services);
    }
}
