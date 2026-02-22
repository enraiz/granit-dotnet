// =============================================================================
// Tests - FoundationSecurityModule
// =============================================================================
<<<<<<< HEAD
// FoundationSecurityModule is an abstractions marker module.
// No services registered — the JWT implementation is in
// Foundation.Authentication.JwtBearer (FoundationJwtBearerModule).
=======
// Vérifie que le module enregistre les services Security via ConfigureServices.
>>>>>>> feature/settings-module
// =============================================================================

using DigitalDynamics.Foundation.Core.Modularity;
using FluentAssertions;
<<<<<<< HEAD
=======
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
>>>>>>> feature/settings-module
using Xunit;

namespace DigitalDynamics.Foundation.Security.Tests;

public sealed class FoundationSecurityModuleTests
{
    [Fact]
<<<<<<< HEAD
    public void FoundationSecurityModule_IsFoundationModule()
    {
        typeof(FoundationSecurityModule).Should().BeAssignableTo<FoundationModule>();
=======
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
>>>>>>> feature/settings-module
    }
}
