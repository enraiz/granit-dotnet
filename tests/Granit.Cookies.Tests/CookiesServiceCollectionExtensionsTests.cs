using FluentAssertions;
using Granit.Cookies.Extensions;
using Granit.Timing.Extensions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Granit.Cookies.Tests;

public sealed class CookiesServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitCookies_RegistersServices()
    {
        ServiceCollection services = new();
        services.AddGranitTiming();
        services.AddGranitCookies(cookies =>
        {
            cookies.UseConsentResolver<FakeConsentResolver>();
        });

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetService<ICookieRegistry>().Should().NotBeNull();
        provider.GetService<IGranitCookieManager>().Should().NotBeNull();
        provider.GetService<IConsentResolver>().Should().NotBeNull();
    }

    [Fact]
    public void AddGranitCookies_RegistersCookiesInRegistry()
    {
        ServiceCollection services = new();
        services.AddGranitCookies(cookies =>
        {
            cookies.RegisterCookie(new("session", CookieCategory.StrictlyNecessary, 1, true, "Session"));
            cookies.RegisterCookie(new("_ga", CookieCategory.Analytics, 730, false, "Google Analytics"));
        });

        ServiceProvider provider = services.BuildServiceProvider();
        ICookieRegistry? registry = provider.GetService<ICookieRegistry>();

        registry.Should().NotBeNull();
        registry!.IsRegistered("session").Should().BeTrue();
        registry.IsRegistered("_ga").Should().BeTrue();
        registry.GetAll().Should().HaveCount(2);
    }

    [Fact]
    public void AddGranitCookies_WithConsentResolver_RegistersResolver()
    {
        ServiceCollection services = new();
        services.AddGranitCookies(cookies =>
        {
            cookies.UseConsentResolver<FakeConsentResolver>();
        });

        ServiceProvider provider = services.BuildServiceProvider();
        IConsentResolver? resolver = provider.GetService<IConsentResolver>();

        resolver.Should().NotBeNull();
        resolver.Should().BeOfType<FakeConsentResolver>();
    }

    [Fact]
    public void AddGranitCookies_NullConfigure_ThrowsArgumentNullException()
    {
        ServiceCollection services = new();

        Action act = () => services.AddGranitCookies(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    private sealed class FakeConsentResolver : IConsentResolver
    {
        public Task<bool> ResolveAsync(Microsoft.AspNetCore.Http.HttpContext httpContext, CookieCategory category) =>
            Task.FromResult(true);
    }
}
