// =============================================================================
// Tests - AuthorizationServiceCollectionExtensions
// =============================================================================
// Vérifie que AddGranitAuthorization enregistre tous les services RBAC
// nécessaires et retourne la collection pour le chaînage.
// =============================================================================

using Granit.Authorization.Abstractions;
using Granit.Authorization.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Granit.Authorization.Tests;

public sealed class AuthorizationServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitAuthorization_RegistersPermissionDefinitionManager()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

        // Act
        services.AddGranitAuthorization();

        // Assert
        using ServiceProvider sp = services.BuildServiceProvider();
        IPermissionDefinitionManager manager = sp.GetRequiredService<IPermissionDefinitionManager>();
        manager.ShouldNotBeNull();
    }

    [Fact]
    public void AddGranitAuthorization_RegistersNullPermissionGrantStore()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

        // Act
        services.AddGranitAuthorization();

        // Assert
        using ServiceProvider sp = services.BuildServiceProvider();
        IPermissionGrantStore store = sp.GetRequiredService<IPermissionGrantStore>();
        store.ShouldNotBeNull();
    }

    [Fact]
    public void AddGranitAuthorization_RegistersPermissionChecker()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddGranitAuthorization();

        // Assert — descriptor registered (transitive dependencies not required here)
        services.ShouldContain(sd => sd.ServiceType == typeof(IPermissionChecker));
    }

    [Fact]
    public void AddGranitAuthorization_RegistersAuthorizationHandler()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddGranitAuthorization();

        // Assert — descriptor registered
        services.ShouldContain(sd => sd.ServiceType == typeof(IAuthorizationHandler));
    }

    [Fact]
    public void AddGranitAuthorization_ReturnsServices_ForChaining()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        IServiceCollection returned = services.AddGranitAuthorization();

        // Assert
        returned.ShouldBeSameAs(services);
    }
}
