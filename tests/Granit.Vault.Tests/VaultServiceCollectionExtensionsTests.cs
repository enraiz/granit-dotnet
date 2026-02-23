// =============================================================================
// Tests - VaultServiceCollectionExtensions
// =============================================================================
// Vérifie que AddGranitVault enregistre les services attendus.
// =============================================================================

using FluentAssertions;
using Granit.Vault.Extensions;
using Granit.Vault.HealthChecks;
using Granit.Vault.Options;
using Granit.Vault.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using VaultSharp;
using Xunit;

namespace Granit.Vault.Tests;

public sealed class VaultServiceCollectionExtensionsTests
{
    private static IConfiguration CreateVaultConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Vault:Address"] = "https://vault.test.com",
                ["Vault:AuthMethod"] = "Token",
                ["Vault:Token"] = "test-token",
                ["Vault:DatabaseMountPoint"] = "database",
                ["Vault:DatabaseRoleName"] = "readwrite"
            })
            .Build();

    [Fact]
    public void AddGranitVault_RegistersVaultOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        IConfiguration config = CreateVaultConfiguration();

        // Act
        services.AddGranitVault(config);

        using ServiceProvider sp = services.BuildServiceProvider();

        // Assert
        VaultOptions options = sp.GetRequiredService<IOptions<VaultOptions>>().Value;
        options.Address.Should().Be("https://vault.test.com");
        options.AuthMethod.Should().Be("Token");
        options.DatabaseRoleName.Should().Be("readwrite");
    }

    [Fact]
    public void AddGranitVault_RegistersVaultClient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        IConfiguration config = CreateVaultConfiguration();

        // Act
        services.AddGranitVault(config);

        using ServiceProvider sp = services.BuildServiceProvider();

        // Assert
        IVaultClient? client = sp.GetService<IVaultClient>();
        client.Should().NotBeNull();
    }

    [Fact]
    public void AddGranitVault_RegistersDatabaseCredentialProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        IConfiguration config = CreateVaultConfiguration();

        // Act
        services.AddGranitVault(config);

        // Assert
        ServiceDescriptor? descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IDatabaseCredentialProvider));

        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGranitVault_RegistersHostedService()
    {
        // Arrange
        var services = new ServiceCollection();
        IConfiguration config = CreateVaultConfiguration();

        // Act
        services.AddGranitVault(config);

        // Assert
        ServiceDescriptor? descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IHostedService));

        descriptor.Should().NotBeNull();
    }

    [Fact]
    public void AddGranitVault_RegistersTransitEncryptionService()
    {
        // Arrange
        var services = new ServiceCollection();
        IConfiguration config = CreateVaultConfiguration();

        // Act
        services.AddGranitVault(config);

        // Assert
        ServiceDescriptor? descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(ITransitEncryptionService));

        descriptor.Should().NotBeNull();
        descriptor!.ImplementationType.Should().Be<TransitEncryptionService>();
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddGranitVaultCheck_RegistersVaultHealthCheck_AsReadinessCheck()
    {
        // Arrange
        ServiceCollection services = new();
        // IVaultClient must be registered so VaultHealthCheck can be resolved
        services.AddSingleton(Substitute.For<IVaultClient>());
        IHealthChecksBuilder builder = services.AddHealthChecks();

        // Act
        builder.AddGranitVaultCheck();

        // Assert — VaultHealthCheck singleton registered
        ServiceDescriptor? healthCheckDescriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(VaultHealthCheck));
        healthCheckDescriptor.Should().NotBeNull();
        healthCheckDescriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton);

        // Assert — HealthCheckRegistration tagged "readiness"
        using ServiceProvider sp = services.BuildServiceProvider();
        HealthCheckServiceOptions opts = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        HealthCheckRegistration? registration = opts.Registrations.FirstOrDefault(r => r.Name == "vault");
        registration.Should().NotBeNull();
        registration!.Tags.Should().Contain("readiness");
    }

    [Fact]
    public void AddGranitVaultCheck_WithCustomName_RegistersCheckWithThatName()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddSingleton(Substitute.For<IVaultClient>());
        IHealthChecksBuilder builder = services.AddHealthChecks();

        // Act
        builder.AddGranitVaultCheck(name: "vault-primary");

        // Assert
        using ServiceProvider sp = services.BuildServiceProvider();
        HealthCheckServiceOptions opts = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        HealthCheckRegistration? registration = opts.Registrations.FirstOrDefault(r => r.Name == "vault-primary");
        registration.Should().NotBeNull();
    }
}
