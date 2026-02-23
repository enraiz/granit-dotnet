// =============================================================================
// Tests - GranitJwtBearerModule
// =============================================================================
// Verifies that the module registers JWT Bearer services via ConfigureServices.
// =============================================================================

using FluentAssertions;
using Granit.Core.Modularity;
using Granit.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Granit.Authentication.JwtBearer.Tests;

public sealed class GranitJwtBearerModuleTests
{
    [Fact]
    public void ConfigureServices_RegistersICurrentUserService()
    {
        // Arrange
        GranitJwtBearerModule module = new();
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
