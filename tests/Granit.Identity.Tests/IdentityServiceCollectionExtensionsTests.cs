using Granit.Identity.Extensions;
using Granit.Identity.Internal;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.Identity.Tests;

public sealed class IdentityServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitIdentity_RegistersNullIdentityProviderAsDefault()
    {
        ServiceCollection services = new();

        services.AddGranitIdentity();

        ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        IIdentityProvider identityProvider = scope.ServiceProvider
            .GetRequiredService<IIdentityProvider>();

        identityProvider.ShouldBeOfType<NullIdentityProvider>();
    }

    [Fact]
    public void AddGranitIdentity_RegistersAsScoped()
    {
        ServiceCollection services = new();

        services.AddGranitIdentity();

        ServiceDescriptor descriptor = services.Single(d => d.ServiceType == typeof(IIdentityProvider));
        descriptor.ImplementationType.ShouldBe(typeof(NullIdentityProvider));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddGranitIdentity_DoesNotOverrideExistingProvider()
    {
        ServiceCollection services = new();
        services.AddScoped<IIdentityProvider>(_ => Substitute.For<IIdentityProvider>());

        services.AddGranitIdentity();

        // TryAddScoped should not replace the already-registered provider.
        services.Count(d => d.ServiceType == typeof(IIdentityProvider)).ShouldBe(1);

        ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        IIdentityProvider identityProvider = scope.ServiceProvider
            .GetRequiredService<IIdentityProvider>();

        identityProvider.ShouldNotBeOfType<NullIdentityProvider>();
    }

    [Fact]
    public void AddGranitIdentity_ReturnsSameServiceCollection()
    {
        ServiceCollection services = new();

        IServiceCollection result = services.AddGranitIdentity();

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddGranitIdentity_CalledTwice_RegistersOnce()
    {
        ServiceCollection services = new();

        services.AddGranitIdentity();
        services.AddGranitIdentity();

        services.Count(d => d.ServiceType == typeof(IIdentityProvider)).ShouldBe(1);
    }

    [Fact]
    public void AddIdentityProvider_ReplacesExistingProvider()
    {
        ServiceCollection services = new();
        services.AddGranitIdentity();

        services.AddIdentityProvider<FakeIdentityProvider>();

        ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        IIdentityProvider identityProvider = scope.ServiceProvider
            .GetRequiredService<IIdentityProvider>();

        identityProvider.ShouldBeOfType<FakeIdentityProvider>();
    }

    [Fact]
    public void AddIdentityProvider_ReplacesEvenWithoutPriorRegistration()
    {
        ServiceCollection services = new();

        services.AddIdentityProvider<FakeIdentityProvider>();

        ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        IIdentityProvider identityProvider = scope.ServiceProvider
            .GetRequiredService<IIdentityProvider>();

        identityProvider.ShouldBeOfType<FakeIdentityProvider>();
    }

    [Fact]
    public void AddIdentityProvider_ReturnsSameServiceCollection()
    {
        ServiceCollection services = new();

        IServiceCollection result = services.AddIdentityProvider<FakeIdentityProvider>();

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddIdentityProvider_RegistersAsScoped()
    {
        ServiceCollection services = new();

        services.AddIdentityProvider<FakeIdentityProvider>();

        ServiceDescriptor descriptor = services.Single(d => d.ServiceType == typeof(IIdentityProvider));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }
}
