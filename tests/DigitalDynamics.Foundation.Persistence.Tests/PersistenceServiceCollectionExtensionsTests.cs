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
    public void AddFoundationPersistence_RegistersAuditedEntityInterceptor()
    {
        // Arrange
        ServiceCollection services = new();
        AddRequiredDependencies(services);

        // Act
        services.AddFoundationPersistence();

        using ServiceProvider sp = services.BuildServiceProvider();
        using IServiceScope scope = sp.CreateScope();

        // Assert
        AuditedEntityInterceptor? interceptor = scope.ServiceProvider.GetService<AuditedEntityInterceptor>();
        interceptor.Should().NotBeNull();
    }

    [Fact]
    public void AddFoundationPersistence_RegistersSoftDeleteInterceptor()
    {
        // Arrange
        ServiceCollection services = new();
        AddRequiredDependencies(services);

        // Act
        services.AddFoundationPersistence();

        using ServiceProvider sp = services.BuildServiceProvider();
        using IServiceScope scope = sp.CreateScope();

        // Assert
        SoftDeleteInterceptor? interceptor = scope.ServiceProvider.GetService<SoftDeleteInterceptor>();
        interceptor.Should().NotBeNull();
    }

    [Fact]
    public void AddFoundationPersistence_InterceptorsAreScoped()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddFoundationPersistence();

        // Assert
        ServiceDescriptor auditDescriptor = services.First(d => d.ServiceType == typeof(AuditedEntityInterceptor));
        auditDescriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);

        ServiceDescriptor softDeleteDescriptor = services.First(d => d.ServiceType == typeof(SoftDeleteInterceptor));
        softDeleteDescriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    private static void AddRequiredDependencies(ServiceCollection services)
    {
        // AuditedEntityInterceptor requires IClock, IGuidGenerator, ICurrentUserService
        services.AddSingleton(NSubstitute.Substitute.For<DigitalDynamics.Foundation.Timing.IClock>());
        services.AddSingleton(NSubstitute.Substitute.For<DigitalDynamics.Foundation.Guids.IGuidGenerator>());
        services.AddSingleton(NSubstitute.Substitute.For<DigitalDynamics.Foundation.Security.ICurrentUserService>());
    }
}
