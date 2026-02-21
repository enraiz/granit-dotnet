// =============================================================================
// Tests - SecurityServiceCollectionExtensions
// =============================================================================
// Vérifie que AddFoundationSecurity enregistre correctement :
//   - Authentification JwtBearer avec Keycloak
//   - Policies d'autorisation (Authenticated, FhirAccess, Admin)
//   - Services CurrentUser et ClaimsTransformation
// =============================================================================

using DigitalDynamics.Foundation.Security;
using DigitalDynamics.Foundation.Security.Authentication;
using DigitalDynamics.Foundation.Security.Extensions;
using DigitalDynamics.Foundation.Security.Options;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace DigitalDynamics.Foundation.Security.Tests;

public sealed class SecurityServiceCollectionExtensionsTests
{
    private static IConfiguration CreateConfiguration(
        string authority = "https://auth.test.com/realms/test",
        string clientId = "test-client",
        string adminRole = "admin")
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Keycloak:Authority"] = authority,
                ["Keycloak:ClientId"] = clientId,
                ["Keycloak:RequireHttpsMetadata"] = "false",
                ["Keycloak:AdminRole"] = adminRole
            })
            .Build();
    }

    [Fact]
    public void AddFoundationSecurity_RegistersKeycloakOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = CreateConfiguration();

        // Act
        services.AddFoundationSecurity(config);

        using var sp = services.BuildServiceProvider();

        // Assert
        var options = sp.GetRequiredService<IOptions<KeycloakOptions>>().Value;
        options.Authority.Should().Be("https://auth.test.com/realms/test");
        options.ClientId.Should().Be("test-client");
        options.RequireHttpsMetadata.Should().BeFalse();
    }

    [Fact]
    public void AddFoundationSecurity_RegistersJwtBearerAuthentication()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = CreateConfiguration();

        // Act
        services.AddFoundationSecurity(config);

        using var sp = services.BuildServiceProvider();

        // Assert
        var jwtOptions = sp.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        jwtOptions.Authority.Should().Be("https://auth.test.com/realms/test");
        jwtOptions.Audience.Should().Be("test-client");
        jwtOptions.RequireHttpsMetadata.Should().BeFalse();
        jwtOptions.TokenValidationParameters.ValidateIssuer.Should().BeTrue();
        jwtOptions.TokenValidationParameters.ValidateAudience.Should().BeTrue();
        jwtOptions.TokenValidationParameters.ValidateLifetime.Should().BeTrue();
        jwtOptions.TokenValidationParameters.NameClaimType.Should().Be("preferred_username");
    }

    [Fact]
    public void AddFoundationSecurity_RegistersAuthorizationPolicies()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = CreateConfiguration();

        // Act
        services.AddFoundationSecurity(config);

        using var sp = services.BuildServiceProvider();

        // Assert
        var authOptions = sp.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        authOptions.GetPolicy("Authenticated").Should().NotBeNull();
        authOptions.GetPolicy("FhirAccess").Should().NotBeNull();
        authOptions.GetPolicy("Admin").Should().NotBeNull();
    }

    [Fact]
    public void AddFoundationSecurity_RegistersCurrentUserService()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = CreateConfiguration();

        // Act
        services.AddFoundationSecurity(config);

        // Assert
        var descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(ICurrentUserService));

        descriptor.Should().NotBeNull();
        descriptor!.ImplementationType.Should().Be<CurrentUserService>();
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddFoundationSecurity_RegistersClaimsTransformation()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = CreateConfiguration();

        // Act
        services.AddFoundationSecurity(config);

        // Assert — AddAuthentication registers NoopClaimsTransformation first,
        // our KeycloakClaimsTransformation is added after
        var descriptors = services
            .Where(d => d.ServiceType == typeof(IClaimsTransformation))
            .ToList();

        descriptors.Should().Contain(d => d.ImplementationType == typeof(KeycloakClaimsTransformation));
    }

    [Fact]
    public void AddFoundationSecurity_RegistersHttpContextAccessor()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = CreateConfiguration();

        // Act
        services.AddFoundationSecurity(config);

        // Assert
        var descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IHttpContextAccessor));

        descriptor.Should().NotBeNull();
    }

    [Fact]
    public void AddFoundationSecurity_WithCustomAudience_UsesAudienceOverClientId()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Keycloak:Authority"] = "https://auth.test.com/realms/test",
                ["Keycloak:ClientId"] = "test-client",
                ["Keycloak:Audience"] = "custom-audience",
                ["Keycloak:RequireHttpsMetadata"] = "false"
            })
            .Build();

        // Act
        services.AddFoundationSecurity(config);

        using var sp = services.BuildServiceProvider();

        // Assert
        var jwtOptions = sp.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        jwtOptions.Audience.Should().Be("custom-audience");
    }
}
