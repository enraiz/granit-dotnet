// =============================================================================
// Tests - FoundationAuthenticationKeycloakModule
// =============================================================================
// Verifies the complete DI wiring via ConfigureServices:
//   - ICurrentUserService resolvable (via dependency on FoundationJwtBearerModule)
//   - KeycloakClaimsTransformation registered
//   - "Admin" policy registered
// =============================================================================

using DigitalDynamics.Foundation.Authentication.JwtBearer;
using DigitalDynamics.Foundation.Authentication.Keycloak.Authentication;
using DigitalDynamics.Foundation.Core.Modularity;
using DigitalDynamics.Foundation.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace DigitalDynamics.Foundation.Authentication.Keycloak.Tests;

public sealed class FoundationAuthenticationKeycloakModuleTests
{
    [Fact]
    public void ConfigureServices_RegistersICurrentUserService()
    {
        // Arrange
        FoundationAuthenticationKeycloakModule module = new();
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration["Keycloak:Authority"] = "https://keycloak.test/realms/test";
        builder.Configuration["Keycloak:ClientId"] = "test-client";
        // Call FoundationJwtBearerModule first (dependency)
        ServiceConfigurationContext context = new(builder.Services, builder.Configuration, builder);
        new FoundationJwtBearerModule().ConfigureServices(context);

        // Act
        module.ConfigureServices(context);

        using ServiceProvider sp = builder.Services.BuildServiceProvider();

        // Assert
        ICurrentUserService? userService = sp.GetService<ICurrentUserService>();
        userService.Should().NotBeNull("inherited from FoundationJwtBearerModule");
    }

    [Fact]
    public void ConfigureServices_RegistersKeycloakClaimsTransformation()
    {
        // Arrange
        FoundationAuthenticationKeycloakModule module = new();
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration["Keycloak:Authority"] = "https://keycloak.test/realms/test";
        builder.Configuration["Keycloak:ClientId"] = "test-client";
        ServiceConfigurationContext context = new(builder.Services, builder.Configuration, builder);
        new FoundationJwtBearerModule().ConfigureServices(context);

        // Act
        module.ConfigureServices(context);

        // Assert
        var descriptors = builder.Services
            .Where(d => d.ServiceType == typeof(IClaimsTransformation))
            .ToList();

        descriptors.Should().Contain(d => d.ImplementationType == typeof(KeycloakClaimsTransformation));
    }

    [Fact]
    public void ConfigureServices_RegistersAdminPolicy()
    {
        // Arrange
        FoundationAuthenticationKeycloakModule module = new();
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration["Keycloak:Authority"] = "https://keycloak.test/realms/test";
        builder.Configuration["Keycloak:ClientId"] = "test-client";
        builder.Configuration["Keycloak:AdminRole"] = "admin";
        ServiceConfigurationContext context = new(builder.Services, builder.Configuration, builder);
        new FoundationJwtBearerModule().ConfigureServices(context);

        // Act
        module.ConfigureServices(context);

        using ServiceProvider sp = builder.Services.BuildServiceProvider();

        // Assert
        AuthorizationOptions authOptions = sp.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
        authOptions.GetPolicy("Admin").Should().NotBeNull();
    }
}
