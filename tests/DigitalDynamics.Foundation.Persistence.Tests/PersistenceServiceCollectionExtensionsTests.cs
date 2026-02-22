// =============================================================================
// Tests - PersistenceServiceCollectionExtensions
// =============================================================================
// Verifies that AddFoundationPersistence registers the EF Core interceptors
// and the IDataFilter service.
// Verifies that AddFoundationDbContextCheck<T> registers a readiness health check.
// =============================================================================

using DigitalDynamics.Foundation.Core.DataFiltering;
using DigitalDynamics.Foundation.MultiTenancy;
using DigitalDynamics.Foundation.Persistence.Extensions;
using DigitalDynamics.Foundation.Persistence.Interceptors;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
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

    [Fact]
    public void AddFoundationPersistence_RegistersDataFilter_AsSingleton()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddFoundationPersistence();

        // Assert
        ServiceDescriptor? descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IDataFilter));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton);
        descriptor.ImplementationType.Should().Be<DataFilter>();
    }

    [Fact]
    public void AddFoundationDbContextCheck_WithDefaultName_RegistersCheckNamedAfterDbContextType()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddDbContext<TestDbContext>(opts => opts.UseInMemoryDatabase("test-health"));
        IHealthChecksBuilder builder = services.AddHealthChecks();

        // Act — no explicit name → defaults to typeof(TContext).Name
        builder.AddFoundationDbContextCheck<TestDbContext>();

        // Assert — registration uses type name and is tagged "readiness"
        using ServiceProvider sp = services.BuildServiceProvider();
        HealthCheckServiceOptions opts = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        HealthCheckRegistration? registration = opts.Registrations.FirstOrDefault(
            r => r.Name == nameof(TestDbContext));
        registration.Should().NotBeNull();
        registration!.Tags.Should().Contain("readiness");
    }

    [Fact]
    public void AddFoundationDbContextCheck_WithCustomName_RegistersCheckWithThatName()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddDbContext<TestDbContext>(opts => opts.UseInMemoryDatabase("test-health-custom"));
        IHealthChecksBuilder builder = services.AddHealthChecks();

        // Act — explicit name
        builder.AddFoundationDbContextCheck<TestDbContext>(name: "database");

        // Assert
        using ServiceProvider sp = services.BuildServiceProvider();
        HealthCheckServiceOptions opts = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        HealthCheckRegistration? registration = opts.Registrations.FirstOrDefault(r => r.Name == "database");
        registration.Should().NotBeNull();
        registration!.Tags.Should().Contain("readiness");
    }

    private static void AddRequiredDependencies(ServiceCollection services)
    {
        // AuditedEntityInterceptor requires IClock, IGuidGenerator, ICurrentUserService, ICurrentTenant
        services.AddSingleton(NSubstitute.Substitute.For<DigitalDynamics.Foundation.Timing.IClock>());
        services.AddSingleton(NSubstitute.Substitute.For<DigitalDynamics.Foundation.Guids.IGuidGenerator>());
        services.AddSingleton(NSubstitute.Substitute.For<DigitalDynamics.Foundation.Security.ICurrentUserService>());
        services.AddSingleton(NSubstitute.Substitute.For<ICurrentTenant>());
    }

    /// <summary>Minimal DbContext for health check registration tests.</summary>
    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options);
}
