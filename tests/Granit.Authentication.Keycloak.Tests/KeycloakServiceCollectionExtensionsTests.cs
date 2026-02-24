// =============================================================================
// Tests - KeycloakServiceCollectionExtensions
// =============================================================================
// Verifies that AddGranitKeycloak correctly registers:
//   - KeycloakOptions from the "Keycloak" section
//   - PostConfigure JWT Bearer (Authority, Audience, NameClaimType)
//   - KeycloakClaimsTransformation
//   - "Admin" policy
// =============================================================================

using FluentAssertions;
using Granit.Authentication.JwtBearer.Extensions;
using Granit.Authentication.Keycloak.Authentication;
using Granit.Authentication.Keycloak.Extensions;
using Granit.Authentication.Keycloak.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Granit.Authentication.Keycloak.Tests;

public sealed class KeycloakServiceCollectionExtensionsTests
{
    private static IConfiguration CreateConfiguration(
        string authority = "https://keycloak.test/realms/test",
        string clientId = "test-client",
        string adminRole = "admin") =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Keycloak:Authority"] = authority,
                ["Keycloak:ClientId"] = clientId,
                ["Keycloak:RequireHttpsMetadata"] = "false",
                ["Keycloak:AdminRole"] = adminRole
            })
            .Build();

    [Fact]
    public void AddGranitKeycloak_RegistersKeycloakOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        IConfiguration config = CreateConfiguration();
        services.AddGranitJwtBearer(config);

        // Act
        services.AddGranitKeycloak(config);

        using ServiceProvider sp = services.BuildServiceProvider();

        // Assert
        KeycloakOptions options = sp.GetRequiredService<IOptions<KeycloakOptions>>().Value;
        options.Authority.Should().Be("https://keycloak.test/realms/test");
        options.ClientId.Should().Be("test-client");
        options.RequireHttpsMetadata.Should().BeFalse();
    }

    [Fact]
    public void AddGranitKeycloak_PostConfiguresJwtBearer_WithKeycloakValues()
    {
        // Arrange
        var services = new ServiceCollection();
        IConfiguration config = CreateConfiguration();
        services.AddGranitJwtBearer(config);

        // Act
        services.AddGranitKeycloak(config);

        using ServiceProvider sp = services.BuildServiceProvider();

        // Assert — PostConfigure overrides the JWT Bearer values
        JwtBearerOptions jwtOptions = sp.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        jwtOptions.Authority.Should().Be("https://keycloak.test/realms/test");
        jwtOptions.Audience.Should().Be("test-client", "ClientId is used as Audience by default");
        jwtOptions.TokenValidationParameters.NameClaimType.Should().Be("preferred_username");
    }

    [Fact]
    public void AddGranitKeycloak_WithCustomAudience_UsesAudienceOverClientId()
    {
        // Arrange
        var services = new ServiceCollection();
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Keycloak:Authority"] = "https://keycloak.test/realms/test",
                ["Keycloak:ClientId"] = "test-client",
                ["Keycloak:Audience"] = "custom-audience",
                ["Keycloak:RequireHttpsMetadata"] = "false"
            })
            .Build();
        services.AddGranitJwtBearer(config);

        // Act
        services.AddGranitKeycloak(config);

        using ServiceProvider sp = services.BuildServiceProvider();

        // Assert
        JwtBearerOptions jwtOptions = sp.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        jwtOptions.Audience.Should().Be("custom-audience");
    }

    [Fact]
    public void AddGranitKeycloak_RegistersClaimsTransformation()
    {
        // Arrange
        var services = new ServiceCollection();
        IConfiguration config = CreateConfiguration();
        services.AddGranitJwtBearer(config);

        // Act
        services.AddGranitKeycloak(config);

        // Assert
        var descriptors = services
            .Where(d => d.ServiceType == typeof(IClaimsTransformation))
            .ToList();

        descriptors.Should().Contain(d => d.ImplementationType == typeof(KeycloakClaimsTransformation));
    }

    [Fact]
    public void AddGranitKeycloak_RegistersAdminPolicy()
    {
        // Arrange
        var services = new ServiceCollection();
        IConfiguration config = CreateConfiguration(adminRole: "superadmin");
        services.AddGranitJwtBearer(config);

        // Act
        services.AddGranitKeycloak(config);

        using ServiceProvider sp = services.BuildServiceProvider();

        // Assert
        AuthorizationOptions authOptions = sp.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
        authOptions.GetPolicy("Admin").Should().NotBeNull();
        authOptions.GetPolicy("Authenticated").Should().NotBeNull("inherited from Granit.Authentication.JwtBearer");
        authOptions.GetPolicy("FhirAccess").Should().BeNull(
            "FhirAccess is application-specific, not part of Granit.Authentication.Keycloak");
    }
}
