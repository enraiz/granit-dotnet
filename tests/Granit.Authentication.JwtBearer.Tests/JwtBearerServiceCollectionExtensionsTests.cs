// =============================================================================
// Tests - JwtBearerServiceCollectionExtensions
// =============================================================================
// Verifies that AddGranitJwtBearer correctly registers:
//   - Generic JwtBearer authentication (section "Authentication")
//   - "Authenticated" authorization policy only
//   - CurrentUser and HttpContextAccessor services
// =============================================================================

using FluentAssertions;
using Granit.Authentication.JwtBearer.Authentication;
using Granit.Authentication.JwtBearer.Extensions;
using Granit.Authentication.JwtBearer.Options;
using Granit.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Granit.Authentication.JwtBearer.Tests;

public sealed class JwtBearerServiceCollectionExtensionsTests
{
    private static IConfiguration CreateConfiguration(
        string authority = "https://auth.test.com/realms/test",
        string audience = "test-client") =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Authority"] = authority,
                ["Authentication:Audience"] = audience,
                ["Authentication:RequireHttpsMetadata"] = "false"
            })
            .Build();

    [Fact]
    public void AddGranitJwtBearer_RegistersJwtBearerAuthOptions()
    {
        // Arrange
        ServiceCollection services = new();
        IConfiguration config = CreateConfiguration();
        services.AddSingleton<IConfiguration>(config);

        // Act
        services.AddGranitJwtBearer();

        using ServiceProvider sp = services.BuildServiceProvider();

        // Assert
        JwtBearerAuthOptions options = sp.GetRequiredService<IOptions<JwtBearerAuthOptions>>().Value;
        options.Authority.Should().Be("https://auth.test.com/realms/test");
        options.Audience.Should().Be("test-client");
        options.RequireHttpsMetadata.Should().BeFalse();
        options.NameClaimType.Should().Be("sub");
    }

    [Fact]
    public void AddGranitJwtBearer_RegistersJwtBearerAuthentication()
    {
        // Arrange
        ServiceCollection services = new();
        IConfiguration config = CreateConfiguration();
        services.AddSingleton<IConfiguration>(config);

        // Act
        services.AddGranitJwtBearer();

        using ServiceProvider sp = services.BuildServiceProvider();

        // Assert
        JwtBearerOptions jwtOptions = sp.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        jwtOptions.Authority.Should().Be("https://auth.test.com/realms/test");
        jwtOptions.Audience.Should().Be("test-client");
        jwtOptions.RequireHttpsMetadata.Should().BeFalse();
        jwtOptions.TokenValidationParameters.ValidateIssuer.Should().BeTrue();
        jwtOptions.TokenValidationParameters.ValidateAudience.Should().BeTrue();
        jwtOptions.TokenValidationParameters.ValidateLifetime.Should().BeTrue();
        jwtOptions.TokenValidationParameters.NameClaimType.Should().Be("sub");
    }

    [Fact]
    public void AddGranitJwtBearer_RegistersOnlyAuthenticatedPolicy()
    {
        // Arrange
        ServiceCollection services = new();
        IConfiguration config = CreateConfiguration();
        services.AddSingleton<IConfiguration>(config);

        // Act
        services.AddGranitJwtBearer();

        using ServiceProvider sp = services.BuildServiceProvider();

        // Assert — only "Authenticated" is registered in Granit.Authentication.JwtBearer (base)
        AuthorizationOptions authOptions = sp.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        authOptions.GetPolicy("Authenticated").Should().NotBeNull();
        authOptions.GetPolicy("Admin").Should().BeNull(
            "Admin is specific to Keycloak, registered by Granit.Authentication.Keycloak");
    }

    [Fact]
    public void AddGranitJwtBearer_RegistersCurrentUserService()
    {
        // Arrange
        ServiceCollection services = new();
        IConfiguration config = CreateConfiguration();
        services.AddSingleton<IConfiguration>(config);

        // Act
        services.AddGranitJwtBearer();

        // Assert
        ServiceDescriptor? descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(ICurrentUserService));

        descriptor.Should().NotBeNull();
        descriptor!.ImplementationType.Should().Be<CurrentUserService>();
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddGranitJwtBearer_RegistersHttpContextAccessor()
    {
        // Arrange
        ServiceCollection services = new();
        IConfiguration config = CreateConfiguration();
        services.AddSingleton<IConfiguration>(config);

        // Act
        services.AddGranitJwtBearer();

        // Assert
        ServiceDescriptor? descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IHttpContextAccessor));

        descriptor.Should().NotBeNull();
    }

    [Fact]
    public void AddGranitJwtBearer_WithCustomNameClaimType_UsesConfiguredNameClaimType()
    {
        // Arrange
        ServiceCollection services = new();
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Authority"] = "https://auth.test.com/realms/test",
                ["Authentication:Audience"] = "test-client",
                ["Authentication:NameClaimType"] = "email"
            })
            .Build();
        services.AddSingleton<IConfiguration>(config);

        // Act
        services.AddGranitJwtBearer();

        using ServiceProvider sp = services.BuildServiceProvider();

        // Assert
        JwtBearerOptions jwtOptions = sp.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        jwtOptions.TokenValidationParameters.NameClaimType.Should().Be("email");
    }
}
