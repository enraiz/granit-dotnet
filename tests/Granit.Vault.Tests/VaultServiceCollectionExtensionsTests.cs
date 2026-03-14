// =============================================================================
// Tests - VaultServiceCollectionExtensions
// =============================================================================
// Vérifie que AddGranitVault enregistre les services attendus.
// =============================================================================

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
using Shouldly;
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
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(CreateVaultConfiguration());

        // Act
        services.AddGranitVault();

        using ServiceProvider sp = services.BuildServiceProvider();

        // Assert
        VaultOptions options = sp.GetRequiredService<IOptions<VaultOptions>>().Value;
        options.Address.ShouldBe("https://vault.test.com");
        options.AuthMethod.ShouldBe("Token");
        options.DatabaseRoleName.ShouldBe("readwrite");
    }

    [Fact]
    public void AddGranitVault_RegistersVaultClient()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(CreateVaultConfiguration());

        // Act
        services.AddGranitVault();

        using ServiceProvider sp = services.BuildServiceProvider();

        // Assert
        IVaultClient? client = sp.GetService<IVaultClient>();
        client.ShouldNotBeNull();
    }

    [Fact]
    public void AddGranitVault_RegistersDatabaseCredentialProvider()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(CreateVaultConfiguration());

        // Act
        services.AddGranitVault();

        // Assert
        ServiceDescriptor? descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IDatabaseCredentialProvider));

        descriptor.ShouldNotBeNull();
        descriptor!.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGranitVault_RegistersHostedService()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(CreateVaultConfiguration());

        // Act
        services.AddGranitVault();

        // Assert
        ServiceDescriptor? descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IHostedService));

        descriptor.ShouldNotBeNull();
    }

    [Fact]
    public void AddGranitVault_RegistersTransitEncryptionService()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(CreateVaultConfiguration());

        // Act
        services.AddGranitVault();

        // Assert
        ServiceDescriptor? descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(ITransitEncryptionService));

        descriptor.ShouldNotBeNull();
        descriptor!.ImplementationType.ShouldBe(typeof(TransitEncryptionService));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddGranitVaultHealthCheck_RegistersVaultHealthCheck_AsReadinessCheck()
    {
        // Arrange
        ServiceCollection services = new();
        // IVaultClient must be registered so VaultHealthCheck can be resolved
        services.AddSingleton(Substitute.For<IVaultClient>());
        IHealthChecksBuilder builder = services.AddHealthChecks();

        // Act
        builder.AddGranitVaultHealthCheck();

        // Assert — VaultHealthCheck singleton registered
        ServiceDescriptor? healthCheckDescriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(VaultHealthCheck));
        healthCheckDescriptor.ShouldNotBeNull();
        healthCheckDescriptor!.Lifetime.ShouldBe(ServiceLifetime.Singleton);

        // Assert — HealthCheckRegistration tagged "readiness" and "startup"
        using ServiceProvider sp = services.BuildServiceProvider();
        HealthCheckServiceOptions opts = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        HealthCheckRegistration? registration = opts.Registrations.FirstOrDefault(r => r.Name == "vault");
        registration.ShouldNotBeNull();
        registration!.Tags.ShouldContain("readiness");
        registration.Tags.ShouldContain("startup");
    }

    [Fact]
    public void AddGranitVaultHealthCheck_WithCustomName_RegistersCheckWithThatName()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddSingleton(Substitute.For<IVaultClient>());
        IHealthChecksBuilder builder = services.AddHealthChecks();

        // Act
        builder.AddGranitVaultHealthCheck(name: "vault-primary");

        // Assert
        using ServiceProvider sp = services.BuildServiceProvider();
        HealthCheckServiceOptions opts = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        HealthCheckRegistration? registration = opts.Registrations.FirstOrDefault(r => r.Name == "vault-primary");
        registration.ShouldNotBeNull();
    }
}
