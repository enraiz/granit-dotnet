using Granit.Authentication.JwtBearer.Extensions;
using Granit.Authentication.Keycloak.BackChannelLogout;
using Granit.Authentication.Keycloak.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Granit.Authentication.Keycloak.Tests.BackChannelLogout;

public sealed class KeycloakBackChannelLogoutRegistrationTests
{
    private static IConfiguration CreateConfiguration(bool backChannelEnabled) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Keycloak:Authority"] = "https://keycloak.test/realms/test",
                ["Keycloak:ClientId"] = "test-client",
                ["Keycloak:RequireHttpsMetadata"] = "false",
                ["Keycloak:BackChannelLogout:Enabled"] = backChannelEnabled.ToString(),
                ["Keycloak:BackChannelLogout:EndpointPath"] = "/auth/back-channel-logout",
                ["Keycloak:BackChannelLogout:SessionRevocationTtl"] = "01:00:00",
            })
            .Build();

    [Fact]
    public void AddGranitKeycloak_BackChannelEnabled_RegistersRevokedSessionStore()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(CreateConfiguration(backChannelEnabled: true));
        services.AddLogging();
        services.AddGranitJwtBearer();

        // Act
        services.AddGranitKeycloak();

        using ServiceProvider sp = services.BuildServiceProvider();

        // Assert
        IRevokedSessionStore? store = sp.GetService<IRevokedSessionStore>();
        store.ShouldNotBeNull();
        store.ShouldBeOfType<DistributedCacheRevokedSessionStore>();
    }

    [Fact]
    public void AddGranitKeycloak_BackChannelEnabled_WiresOnTokenValidated()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(CreateConfiguration(backChannelEnabled: true));
        services.AddLogging();
        services.AddGranitJwtBearer();

        // Act
        services.AddGranitKeycloak();

        using ServiceProvider sp = services.BuildServiceProvider();

        // Assert
        JwtBearerOptions jwtOptions = sp.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        jwtOptions.Events.ShouldNotBeNull();
        jwtOptions.Events.OnTokenValidated.ShouldNotBeNull();
    }

    [Fact]
    public void AddGranitKeycloak_BackChannelEnabled_OnTokenValidatedDiffersFromDefault()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(CreateConfiguration(backChannelEnabled: true));
        services.AddLogging();
        services.AddGranitJwtBearer();

        // Act
        services.AddGranitKeycloak();

        using ServiceProvider sp = services.BuildServiceProvider();

        // Assert — When enabled, OnTokenValidated should be our custom delegate,
        // distinct from the framework default.
        JwtBearerOptions jwtOptions = sp.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        JwtBearerEvents defaultEvents = new();
        jwtOptions.Events.ShouldNotBeNull();
        jwtOptions.Events.OnTokenValidated.ShouldNotBe(defaultEvents.OnTokenValidated);
    }

    [Fact]
    public void AddGranitKeycloak_AlwaysRegistersStore_EvenWhenDisabled()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(CreateConfiguration(backChannelEnabled: false));
        services.AddLogging();
        services.AddGranitJwtBearer();

        // Act
        services.AddGranitKeycloak();

        using ServiceProvider sp = services.BuildServiceProvider();

        // Assert
        sp.GetService<IRevokedSessionStore>().ShouldNotBeNull();
    }
}
