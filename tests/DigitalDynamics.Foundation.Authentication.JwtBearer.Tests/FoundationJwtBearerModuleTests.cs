// =============================================================================
// Tests - FoundationJwtBearerModule
// =============================================================================
// Vérifie que le module enregistre les services JWT Bearer via ConfigureServices.
// =============================================================================

using DigitalDynamics.Foundation.Core.Modularity;
using DigitalDynamics.Foundation.Security;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace DigitalDynamics.Foundation.Authentication.JwtBearer.Tests;

public sealed class FoundationJwtBearerModuleTests
{
    [Fact]
    public void ConfigureServices_RegistersICurrentUserService()
    {
        // Arrange
        FoundationJwtBearerModule module = new();
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration["Authentication:Authority"] = "https://auth.test/realms/test";
        builder.Configuration["Authentication:Audience"] = "test-client";
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
