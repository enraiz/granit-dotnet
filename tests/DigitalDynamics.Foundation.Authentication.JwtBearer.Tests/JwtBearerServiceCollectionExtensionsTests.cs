// =============================================================================
// Tests - JwtBearerServiceCollectionExtensions
// =============================================================================
// Vérifie que AddFoundationJwtBearer enregistre correctement :
//   - Authentification JwtBearer générique (section "Authentication")
//   - Policy d'autorisation "Authenticated" uniquement
//   - Services CurrentUser et HttpContextAccessor
// =============================================================================

using DigitalDynamics.Foundation.Authentication.JwtBearer.Authentication;
using DigitalDynamics.Foundation.Authentication.JwtBearer.Extensions;
using DigitalDynamics.Foundation.Authentication.JwtBearer.Options;
using DigitalDynamics.Foundation.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace DigitalDynamics.Foundation.Authentication.JwtBearer.Tests;

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
    public void AddFoundationJwtBearer_RegistersJwtBearerAuthOptions()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        IConfiguration config = CreateConfiguration();

        // Act
        services.AddFoundationJwtBearer(config);

        using ServiceProvider sp = services.BuildServiceProvider();

        // Assert
        JwtBearerAuthOptions options = sp.GetRequiredService<IOptions<JwtBearerAuthOptions>>().Value;
        options.Authority.Should().Be("https://auth.test.com/realms/test");
        options.Audience.Should().Be("test-client");
        options.RequireHttpsMetadata.Should().BeFalse();
        options.NameClaimType.Should().Be("sub");
    }

    [Fact]
    public void AddFoundationJwtBearer_RegistersJwtBearerAuthentication()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        IConfiguration config = CreateConfiguration();

        // Act
        services.AddFoundationJwtBearer(config);

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
    public void AddFoundationJwtBearer_RegistersOnlyAuthenticatedPolicy()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        IConfiguration config = CreateConfiguration();

        // Act
        services.AddFoundationJwtBearer(config);

        using ServiceProvider sp = services.BuildServiceProvider();

        // Assert — seule "Authenticated" est enregistrée dans Foundation.Authentication.JwtBearer (base)
        AuthorizationOptions authOptions = sp.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        authOptions.GetPolicy("Authenticated").Should().NotBeNull();
        authOptions.GetPolicy("Admin").Should().BeNull(
            "Admin est spécifique à Keycloak, enregistré par Foundation.Authentication.Keycloak");
    }

    [Fact]
    public void AddFoundationJwtBearer_RegistersCurrentUserService()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        IConfiguration config = CreateConfiguration();

        // Act
        services.AddFoundationJwtBearer(config);

        // Assert
        ServiceDescriptor? descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(ICurrentUserService));

        descriptor.Should().NotBeNull();
        descriptor!.ImplementationType.Should().Be<CurrentUserService>();
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddFoundationJwtBearer_RegistersHttpContextAccessor()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        IConfiguration config = CreateConfiguration();

        // Act
        services.AddFoundationJwtBearer(config);

        // Assert
        ServiceDescriptor? descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IHttpContextAccessor));

        descriptor.Should().NotBeNull();
    }

    [Fact]
    public void AddFoundationJwtBearer_WithCustomNameClaimType_UsesConfiguredNameClaimType()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        IConfigurationRoot config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Authority"] = "https://auth.test.com/realms/test",
                ["Authentication:Audience"] = "test-client",
                ["Authentication:NameClaimType"] = "email"
            })
            .Build();

        // Act
        services.AddFoundationJwtBearer(config);

        using ServiceProvider sp = services.BuildServiceProvider();

        // Assert
        JwtBearerOptions jwtOptions = sp.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        jwtOptions.TokenValidationParameters.NameClaimType.Should().Be("email");
    }
}
