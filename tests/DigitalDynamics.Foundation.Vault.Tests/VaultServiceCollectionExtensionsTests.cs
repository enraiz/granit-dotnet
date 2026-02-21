// =============================================================================
// Tests - VaultServiceCollectionExtensions
// =============================================================================
// Vérifie que AddFoundationVault enregistre les services attendus.
// =============================================================================

using DigitalDynamics.Foundation.Vault.Extensions;
using DigitalDynamics.Foundation.Vault.Options;
using DigitalDynamics.Foundation.Vault.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VaultSharp;
using Xunit;

namespace DigitalDynamics.Foundation.Vault.Tests;

public sealed class VaultServiceCollectionExtensionsTests
{
    private static IConfiguration CreateVaultConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Vault:Address"] = "https://vault.test.com",
                ["Vault:AuthMethod"] = "Token",
                ["Vault:Token"] = "test-token",
                ["Vault:DatabaseMountPoint"] = "database",
                ["Vault:DatabaseRoleName"] = "readwrite"
            })
            .Build();
    }

    [Fact]
    public void AddFoundationVault_RegistersVaultOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = CreateVaultConfiguration();

        // Act
        services.AddFoundationVault(config);

        using var sp = services.BuildServiceProvider();

        // Assert
        var options = sp.GetRequiredService<IOptions<VaultOptions>>().Value;
        options.Address.Should().Be("https://vault.test.com");
        options.AuthMethod.Should().Be("Token");
        options.DatabaseRoleName.Should().Be("readwrite");
    }

    [Fact]
    public void AddFoundationVault_RegistersVaultClient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var config = CreateVaultConfiguration();

        // Act
        services.AddFoundationVault(config);

        using var sp = services.BuildServiceProvider();

        // Assert
        var client = sp.GetService<IVaultClient>();
        client.Should().NotBeNull();
    }

    [Fact]
    public void AddFoundationVault_RegistersDatabaseCredentialProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = CreateVaultConfiguration();

        // Act
        services.AddFoundationVault(config);

        // Assert
        var descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IDatabaseCredentialProvider));

        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddFoundationVault_RegistersHostedService()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = CreateVaultConfiguration();

        // Act
        services.AddFoundationVault(config);

        // Assert
        var descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IHostedService));

        descriptor.Should().NotBeNull();
    }

    [Fact]
    public void AddFoundationVault_RegistersTransitEncryptionService()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = CreateVaultConfiguration();

        // Act
        services.AddFoundationVault(config);

        // Assert
        var descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(ITransitEncryptionService));

        descriptor.Should().NotBeNull();
        descriptor!.ImplementationType.Should().Be<TransitEncryptionService>();
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }
}
