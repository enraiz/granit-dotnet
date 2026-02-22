// =============================================================================
// Tests - AuthorizationServiceCollectionExtensions
// =============================================================================
// Vérifie que AddFoundationAuthorization enregistre tous les services RBAC
// nécessaires et retourne la collection pour le chaînage.
// =============================================================================

using DigitalDynamics.Foundation.Authorization.Abstractions;
using DigitalDynamics.Foundation.Authorization.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DigitalDynamics.Foundation.Authorization.Tests;

public sealed class AuthorizationServiceCollectionExtensionsTests
{
    private static IConfiguration EmptyConfiguration =>
        new ConfigurationBuilder().Build();

    [Fact]
    public void AddFoundationAuthorization_RegistersPermissionDefinitionManager()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddFoundationAuthorization(EmptyConfiguration);

        // Assert
        using ServiceProvider sp = services.BuildServiceProvider();
        IPermissionDefinitionManager manager = sp.GetRequiredService<IPermissionDefinitionManager>();
        manager.Should().NotBeNull();
    }

    [Fact]
    public void AddFoundationAuthorization_RegistersNullPermissionGrantStore()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddFoundationAuthorization(EmptyConfiguration);

        // Assert
        using ServiceProvider sp = services.BuildServiceProvider();
        IPermissionGrantStore store = sp.GetRequiredService<IPermissionGrantStore>();
        store.Should().NotBeNull();
    }

    [Fact]
    public void AddFoundationAuthorization_RegistersPermissionChecker()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddFoundationAuthorization(EmptyConfiguration);

        // Assert — descriptor registered (transitive dependencies not required here)
        services.Should().Contain(sd => sd.ServiceType == typeof(IPermissionChecker));
    }

    [Fact]
    public void AddFoundationAuthorization_RegistersAuthorizationHandler()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddFoundationAuthorization(EmptyConfiguration);

        // Assert — descriptor registered
        services.Should().Contain(sd => sd.ServiceType == typeof(IAuthorizationHandler));
    }

    [Fact]
    public void AddFoundationAuthorization_ReturnsServices_ForChaining()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        IServiceCollection returned = services.AddFoundationAuthorization(EmptyConfiguration);

        // Assert
        returned.Should().BeSameAs(services);
    }
}
