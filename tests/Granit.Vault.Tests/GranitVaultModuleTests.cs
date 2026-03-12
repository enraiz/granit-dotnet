// =============================================================================
// Tests - GranitVaultModule
// =============================================================================
// Vérifie que le module :
//   - Skip l'enregistrement en environnement Development
//   - Enregistre les services Vault en environnement Production
// =============================================================================

using Granit.Core.Modularity;
using Granit.Vault.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using Xunit;

namespace Granit.Vault.Tests;

public sealed class GranitVaultModuleTests
{
    [Fact]
    public void IsEnabled_InDevelopment_ReturnsFalse()
    {
        // Arrange
        GranitVaultModule module = new();
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(["--environment", "Development"]);
        ServiceConfigurationContext context = new(
            builder.Services,
            builder.Configuration,
            builder);

        // Act & Assert — module disabled in Development (no Vault required locally)
        module.IsEnabled(context).ShouldBeFalse();
    }

    [Fact]
    public void ConfigureServices_InProduction_RegistersVaultServices()
    {
        // Arrange
        GranitVaultModule module = new();
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
        vaultDescriptor.ShouldNotBeNull();
    }
}
