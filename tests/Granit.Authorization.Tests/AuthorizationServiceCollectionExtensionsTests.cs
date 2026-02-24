// =============================================================================
// Tests - AuthorizationServiceCollectionExtensions
// =============================================================================
// Vérifie que AddGranitAuthorization enregistre tous les services RBAC
// nécessaires et retourne la collection pour le chaînage.
// =============================================================================

using FluentAssertions;
using Granit.Authorization.Abstractions;
using Granit.Authorization.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Granit.Authorization.Tests;

public sealed class AuthorizationServiceCollectionExtensionsTests
{
    private static IConfiguration EmptyConfiguration =>
        new ConfigurationBuilder().Build();

    [Fact]
    public void AddGranitAuthorization_RegistersPermissionDefinitionManager()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddGranitAuthorization(EmptyConfiguration);

        // Assert
        using ServiceProvider sp = services.BuildServiceProvider();
        IPermissionDefinitionManager manager = sp.GetRequiredService<IPermissionDefinitionManager>();
        manager.Should().NotBeNull();
    }

    [Fact]
    public void AddGranitAuthorization_RegistersNullPermissionGrantStore()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddGranitAuthorization(EmptyConfiguration);

        // Assert
        using ServiceProvider sp = services.BuildServiceProvider();
        IPermissionGrantStore store = sp.GetRequiredService<IPermissionGrantStore>();
        store.Should().NotBeNull();
    }

    [Fact]
    public void AddGranitAuthorization_RegistersPermissionChecker()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddGranitAuthorization(EmptyConfiguration);

        // Assert — descriptor registered (transitive dependencies not required here)
        services.Should().Contain(sd => sd.ServiceType == typeof(IPermissionChecker));
    }

    [Fact]
    public void AddGranitAuthorization_RegistersAuthorizationHandler()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddGranitAuthorization(EmptyConfiguration);

        // Assert — descriptor registered
        services.Should().Contain(sd => sd.ServiceType == typeof(IAuthorizationHandler));
    }

    [Fact]
    public void AddGranitAuthorization_ReturnsServices_ForChaining()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        IServiceCollection returned = services.AddGranitAuthorization(EmptyConfiguration);

        // Assert
        returned.Should().BeSameAs(services);
    }
}
