// =============================================================================
// Tests - PersistenceServiceCollectionExtensions
// =============================================================================
// Vérifie que AddFoundationPersistence enregistre les intercepteurs EF Core.
// =============================================================================

using DigitalDynamics.Foundation.Persistence.Extensions;
using DigitalDynamics.Foundation.Persistence.Interceptors;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DigitalDynamics.Foundation.Persistence.Tests;

public sealed class PersistenceServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFoundationPersistence_RegistersAuditableEntityInterceptor()
    {
        // Arrange
        var services = new ServiceCollection();
        AddRequiredDependencies(services);

        // Act
        services.AddFoundationPersistence();

        using var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();

        // Assert
        var interceptor = scope.ServiceProvider.GetService<AuditableEntityInterceptor>();
        interceptor.Should().NotBeNull();
    }

    [Fact]
    public void AddFoundationPersistence_RegistersSoftDeleteInterceptor()
    {
        // Arrange
        var services = new ServiceCollection();
        AddRequiredDependencies(services);

        // Act
        services.AddFoundationPersistence();

        using var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();

        // Assert
        var interceptor = scope.ServiceProvider.GetService<SoftDeleteInterceptor>();
        interceptor.Should().NotBeNull();
    }

    [Fact]
    public void AddFoundationPersistence_InterceptorsAreScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddFoundationPersistence();

        // Assert
        var auditDescriptor = services.First(d => d.ServiceType == typeof(AuditableEntityInterceptor));
        auditDescriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);

        var softDeleteDescriptor = services.First(d => d.ServiceType == typeof(SoftDeleteInterceptor));
        softDeleteDescriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    private static void AddRequiredDependencies(ServiceCollection services)
    {
        // AuditableEntityInterceptor requires IClock, IGuidGenerator, ICurrentUserService
        services.AddSingleton(NSubstitute.Substitute.For<DigitalDynamics.Foundation.Timing.IClock>());
        services.AddSingleton(NSubstitute.Substitute.For<DigitalDynamics.Foundation.Guids.IGuidGenerator>());
        services.AddSingleton(NSubstitute.Substitute.For<DigitalDynamics.Foundation.Security.ICurrentUserService>());
    }
}
