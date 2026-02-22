// =============================================================================
// Tests - FoundationVaultModule
// =============================================================================
// Vérifie que le module :
//   - Skip l'enregistrement en environnement Development
//   - Enregistre les services Vault en environnement Production
// =============================================================================

using DigitalDynamics.Foundation.Core.Modularity;
using DigitalDynamics.Foundation.Vault.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace DigitalDynamics.Foundation.Vault.Tests;

public sealed class FoundationVaultModuleTests
{
    [Fact]
    public void ConfigureServices_InDevelopment_SkipsRegistration()
    {
        // Arrange
        FoundationVaultModule module = new();
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(["--environment", "Development"]);
        ServiceConfigurationContext context = new(
            builder.Services,
            builder.Configuration,
            builder);

        // Act
        module.ConfigureServices(context);

        using ServiceProvider sp = builder.Services.BuildServiceProvider();

        // Assert — aucun service Vault ne doit être enregistré
        ITransitEncryptionService? transitService = sp.GetService<ITransitEncryptionService>();
        transitService.Should().BeNull();
    }

    [Fact]
    public void ConfigureServices_InProduction_RegistersVaultServices()
    {
        // Arrange
        FoundationVaultModule module = new();
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(["--environment", "Production"]);
        builder.Configuration["Vault:Address"] = "https://vault.test:8200";
        builder.Configuration["Vault:RoleName"] = "test-role";
        ServiceConfigurationContext context = new(
            builder.Services,
            builder.Configuration,
            builder);

        // Act
        module.ConfigureServices(context);

        // Assert — les services Vault doivent être enregistrés
        ServiceDescriptor? vaultDescriptor = builder.Services.FirstOrDefault(
            d => d.ServiceType == typeof(VaultClientFactory));
        vaultDescriptor.Should().NotBeNull();
    }
}
