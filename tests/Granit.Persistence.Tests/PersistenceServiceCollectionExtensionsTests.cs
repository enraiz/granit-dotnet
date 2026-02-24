// =============================================================================
// Tests - PersistenceServiceCollectionExtensions
// =============================================================================
// Verifies that AddGranitPersistence registers the EF Core interceptors
// and the IDataFilter service.
// Verifies that AddGranitDbContextCheck<T> registers a readiness health check.
// =============================================================================

using FluentAssertions;
using Granit.Core.DataFiltering;
using Granit.Core.MultiTenancy;
using Granit.Persistence.Extensions;
using Granit.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Xunit;

namespace Granit.Persistence.Tests;

public sealed class PersistenceServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitPersistence_RegistersAuditedEntityInterceptor()
    {
        // Arrange
        ServiceCollection services = new();
        AddRequiredDependencies(services);

        // Act
        services.AddGranitPersistence();

        using ServiceProvider sp = services.BuildServiceProvider();
        using IServiceScope scope = sp.CreateScope();

        // Assert
        AuditedEntityInterceptor? interceptor = scope.ServiceProvider.GetService<AuditedEntityInterceptor>();
        interceptor.Should().NotBeNull();
    }

    [Fact]
    public void AddGranitPersistence_RegistersSoftDeleteInterceptor()
    {
        // Arrange
        ServiceCollection services = new();
        AddRequiredDependencies(services);

        // Act
        services.AddGranitPersistence();

        using ServiceProvider sp = services.BuildServiceProvider();
        using IServiceScope scope = sp.CreateScope();

        // Assert
        SoftDeleteInterceptor? interceptor = scope.ServiceProvider.GetService<SoftDeleteInterceptor>();
        interceptor.Should().NotBeNull();
    }

    [Fact]
    public void AddGranitPersistence_InterceptorsAreScoped()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddGranitPersistence();

        // Assert
        ServiceDescriptor auditDescriptor = services.First(d => d.ServiceType == typeof(AuditedEntityInterceptor));
        auditDescriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);

        ServiceDescriptor softDeleteDescriptor = services.First(d => d.ServiceType == typeof(SoftDeleteInterceptor));
        softDeleteDescriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddGranitPersistence_RegistersDataFilter_AsSingleton()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddGranitPersistence();

        // Assert
        ServiceDescriptor? descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IDataFilter));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton);
        descriptor.ImplementationType.Should().Be<DataFilter>();
    }

    [Fact]
    public void AddGranitDbContextCheck_WithDefaultName_RegistersCheckNamedAfterDbContextType()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddDbContext<TestDbContext>(opts => opts.UseInMemoryDatabase("test-health"));
        IHealthChecksBuilder builder = services.AddHealthChecks();

        // Act — no explicit name → defaults to typeof(TContext).Name
        builder.AddGranitDbContextCheck<TestDbContext>();

        // Assert — registration uses type name and is tagged "readiness"
        using ServiceProvider sp = services.BuildServiceProvider();
        HealthCheckServiceOptions opts = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        HealthCheckRegistration? registration = opts.Registrations.FirstOrDefault(
            r => r.Name == nameof(TestDbContext));
        registration.Should().NotBeNull();
        registration!.Tags.Should().Contain("readiness");
    }

    [Fact]
    public void AddGranitDbContextCheck_WithCustomName_RegistersCheckWithThatName()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddDbContext<TestDbContext>(opts => opts.UseInMemoryDatabase("test-health-custom"));
        IHealthChecksBuilder builder = services.AddHealthChecks();

        // Act — explicit name
        builder.AddGranitDbContextCheck<TestDbContext>(name: "database");

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
        services.AddSingleton(NSubstitute.Substitute.For<Granit.Timing.IClock>());
        services.AddSingleton(NSubstitute.Substitute.For<Granit.Guids.IGuidGenerator>());
        services.AddSingleton(NSubstitute.Substitute.For<Granit.Security.ICurrentUserService>());
        services.AddSingleton(NSubstitute.Substitute.For<ICurrentTenant>());
    }

    /// <summary>Minimal DbContext for health check registration tests.</summary>
    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options);
}
