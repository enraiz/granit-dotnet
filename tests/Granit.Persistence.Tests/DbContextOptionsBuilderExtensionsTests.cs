using Granit.Core.MultiTenancy;
using Granit.Persistence.Extensions;
using Granit.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Granit.Persistence.Tests;

public sealed class DbContextOptionsBuilderExtensionsTests
{
    [Fact]
    public void UseGranitInterceptors_AddsAllRegisteredInterceptors()
    {
        // Arrange
        ServiceCollection services = new();
        AddRequiredDependencies(services);
        services.AddGranitPersistence();

        using ServiceProvider sp = services.BuildServiceProvider();
        using IServiceScope scope = sp.CreateScope();

        DbContextOptionsBuilder builder = new();

        // Act
        builder.UseGranitInterceptors(scope.ServiceProvider);

        // Assert — all 4 interceptors should be present
        DbContextOptions options = builder.Options;
        IEnumerable<IInterceptor> interceptors = options.Extensions
            .OfType<Microsoft.EntityFrameworkCore.Infrastructure.CoreOptionsExtension>()
            .SelectMany(e => e.Interceptors ?? []);

        interceptors.ShouldContain(i => i is AuditedEntityInterceptor);
        interceptors.ShouldContain(i => i is VersioningInterceptor);
        interceptors.ShouldContain(i => i is DomainEventDispatcherInterceptor);
        interceptors.ShouldContain(i => i is SoftDeleteInterceptor);
    }

    [Fact]
    public void UseGranitInterceptors_SkipsUnregisteredInterceptors()
    {
        // Arrange — empty service provider with no interceptors registered
        ServiceCollection services = new();
        using ServiceProvider sp = services.BuildServiceProvider();

        DbContextOptionsBuilder builder = new();

        // Act — should not throw
        builder.UseGranitInterceptors(sp);

        // Assert — no interceptors added
        DbContextOptions options = builder.Options;
        IEnumerable<IInterceptor> interceptors = options.Extensions
            .OfType<Microsoft.EntityFrameworkCore.Infrastructure.CoreOptionsExtension>()
            .SelectMany(e => e.Interceptors ?? []);

        interceptors.ShouldBeEmpty();
    }

    [Fact]
    public void UseGranitInterceptors_ThrowsOnNullOptions()
    {
        using ServiceProvider sp = new ServiceCollection().BuildServiceProvider();
        Should.Throw<ArgumentNullException>(() =>
            DbContextOptionsBuilderExtensions.UseGranitInterceptors(null!, sp));
    }

    [Fact]
    public void UseGranitInterceptors_ThrowsOnNullServiceProvider()
    {
        DbContextOptionsBuilder builder = new();
        Should.Throw<ArgumentNullException>(() =>
            builder.UseGranitInterceptors(null!));
    }

    [Fact]
    public void UseGranitInterceptors_ReturnsBuilderForChaining()
    {
        // Arrange
        using ServiceProvider sp = new ServiceCollection().BuildServiceProvider();
        DbContextOptionsBuilder builder = new();

        // Act
        DbContextOptionsBuilder result = builder.UseGranitInterceptors(sp);

        // Assert
        result.ShouldBeSameAs(builder);
    }

    private static void AddRequiredDependencies(ServiceCollection services)
    {
        services.AddSingleton(NSubstitute.Substitute.For<Granit.Timing.IClock>());
        services.AddSingleton(NSubstitute.Substitute.For<Granit.Guids.IGuidGenerator>());
        services.AddSingleton(NSubstitute.Substitute.For<Granit.Security.ICurrentUserService>());
        services.AddSingleton(NSubstitute.Substitute.For<ICurrentTenant>());
    }
}
