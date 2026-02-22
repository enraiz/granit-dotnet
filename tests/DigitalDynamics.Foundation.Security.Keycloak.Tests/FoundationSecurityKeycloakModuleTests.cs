// =============================================================================
// Tests - FoundationSecurityKeycloakModule
// =============================================================================
// Vérifie le câblage DI complet via ConfigureServices :
//   - ICurrentUserService résolvable (via dépendance sur FoundationSecurityModule)
//   - KeycloakClaimsTransformation enregistrée
//   - Policy "Admin" enregistrée
// =============================================================================

using DigitalDynamics.Foundation.Core.Modularity;
using DigitalDynamics.Foundation.Security.Keycloak.Authentication;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace DigitalDynamics.Foundation.Security.Keycloak.Tests;

public sealed class FoundationSecurityKeycloakModuleTests
{
    [Fact]
    public void ConfigureServices_RegistersICurrentUserService()
    {
        // Arrange
        FoundationSecurityKeycloakModule module = new();
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration["Keycloak:Authority"] = "https://keycloak.test/realms/test";
        builder.Configuration["Keycloak:ClientId"] = "test-client";
        // Appeler d'abord FoundationSecurityModule (dépendance)
        ServiceConfigurationContext context = new(builder.Services, builder.Configuration, builder);
        new FoundationSecurityModule().ConfigureServices(context);

        // Act
        module.ConfigureServices(context);

        using ServiceProvider sp = builder.Services.BuildServiceProvider();

        // Assert
        ICurrentUserService? userService = sp.GetService<ICurrentUserService>();
        userService.Should().NotBeNull("hérité de FoundationSecurityModule");
    }

    [Fact]
    public void ConfigureServices_RegistersKeycloakClaimsTransformation()
    {
        // Arrange
        FoundationSecurityKeycloakModule module = new();
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration["Keycloak:Authority"] = "https://keycloak.test/realms/test";
        builder.Configuration["Keycloak:ClientId"] = "test-client";
        ServiceConfigurationContext context = new(builder.Services, builder.Configuration, builder);
        new FoundationSecurityModule().ConfigureServices(context);

        // Act
        module.ConfigureServices(context);

        // Assert
        List<ServiceDescriptor> descriptors = builder.Services
            .Where(d => d.ServiceType == typeof(IClaimsTransformation))
            .ToList();

        descriptors.Should().Contain(d => d.ImplementationType == typeof(KeycloakClaimsTransformation));
    }

    [Fact]
    public void ConfigureServices_RegistersAdminPolicy()
    {
        // Arrange
        FoundationSecurityKeycloakModule module = new();
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration["Keycloak:Authority"] = "https://keycloak.test/realms/test";
        builder.Configuration["Keycloak:ClientId"] = "test-client";
        builder.Configuration["Keycloak:AdminRole"] = "admin";
        ServiceConfigurationContext context = new(builder.Services, builder.Configuration, builder);
        new FoundationSecurityModule().ConfigureServices(context);

        // Act
        module.ConfigureServices(context);

        using ServiceProvider sp = builder.Services.BuildServiceProvider();

        // Assert
        AuthorizationOptions authOptions = sp.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
        authOptions.GetPolicy("Admin").Should().NotBeNull();
    }
}
