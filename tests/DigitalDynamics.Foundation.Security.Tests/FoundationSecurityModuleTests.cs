// =============================================================================
// Tests - FoundationSecurityModule
// =============================================================================
// Vérifie que le module enregistre les services Security via ConfigureServices.
// =============================================================================

using DigitalDynamics.Foundation.Core.Modularity;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace DigitalDynamics.Foundation.Security.Tests;

public sealed class FoundationSecurityModuleTests
{
    [Fact]
    public void ConfigureServices_RegistersSecurityServices()
    {
        // Arrange
        FoundationSecurityModule module = new();
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration["Keycloak:Authority"] = "https://keycloak.test/realms/test";
        builder.Configuration["Keycloak:Audience"] = "test-client";
        ServiceConfigurationContext context = new(
            builder.Services,
            builder.Configuration,
            builder);

        // Act
        module.ConfigureServices(context);

        using ServiceProvider sp = builder.Services.BuildServiceProvider();

        // Assert
        ICurrentUserService? userService = sp.GetService<ICurrentUserService>();
        userService.Should().NotBeNull();
    }
}
