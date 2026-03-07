using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.Localization.Tests;

public sealed class CachedLocalizationOverrideStoreTests
{
    // -------------------------------------------------------------------------
    // Test infrastructure
    // -------------------------------------------------------------------------

    private static CachedLocalizationOverrideStore BuildStore(
        ILocalizationOverrideStore inner,
        TimeSpan? cacheTtl = null)
    {
        ServiceCollection services = new();
        services.AddKeyedScoped<ILocalizationOverrideStore>(
            CachedLocalizationOverrideStore.RawStoreKey, (_, _) => inner);

        IMemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());
        IOptions<LocalizationOverridesCacheOptions> options = Options.Create(
            new LocalizationOverridesCacheOptions { CacheTtl = cacheTtl ?? TimeSpan.FromMinutes(5) });

        ServiceProvider sp = services.BuildServiceProvider();
        IServiceScopeFactory scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

        return new CachedLocalizationOverrideStore(memoryCache, options, scopeFactory, sp);
    }

    // -------------------------------------------------------------------------
    // GetOverridesAsync — cache miss / hit
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetOverridesAsync_CacheMiss_CallsInnerStore()
    {
        ILocalizationOverrideStore inner = Substitute.For<ILocalizationOverrideStore>();
        inner.GetOverridesAsync("TestApp", "fr", Arg.Any<CancellationToken>())
             .ReturnsForAnyArgs(Task.FromResult<IReadOnlyDictionary<string, string>>(
                 new Dictionary<string, string> { ["Key1"] = "Valeur1" }));

        CachedLocalizationOverrideStore store = BuildStore(inner);
        IReadOnlyDictionary<string, string> result =
            await store.GetOverridesAsync("TestApp", "fr", TestContext.Current.CancellationToken);

        result["Key1"].ShouldBe("Valeur1");
        await inner.Received(1).GetOverridesAsync("TestApp", "fr", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetOverridesAsync_CacheHit_DoesNotCallInnerStoreAgain()
    {
        ILocalizationOverrideStore inner = Substitute.For<ILocalizationOverrideStore>();
        inner.GetOverridesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
             .ReturnsForAnyArgs(Task.FromResult<IReadOnlyDictionary<string, string>>(
                 new Dictionary<string, string> { ["Key1"] = "Valeur1" }));

        CachedLocalizationOverrideStore store = BuildStore(inner);

        await store.GetOverridesAsync("TestApp", "fr", TestContext.Current.CancellationToken);
        await store.GetOverridesAsync("TestApp", "fr", TestContext.Current.CancellationToken);

        await inner.Received(1).GetOverridesAsync("TestApp", "fr", Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // SetOverrideAsync — forwards + invalidates cache
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SetOverrideAsync_ForwardsToInnerStore()
    {
        ILocalizationOverrideStore inner = Substitute.For<ILocalizationOverrideStore>();
        inner.SetOverrideAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
             .ReturnsForAnyArgs(Task.CompletedTask);

        CachedLocalizationOverrideStore store = BuildStore(inner);
        await store.SetOverrideAsync(
            "TestApp", "fr", "Patient.Title", "Bénéficiaire",
            TestContext.Current.CancellationToken);

        await inner.Received(1).SetOverrideAsync(
            "TestApp", "fr", "Patient.Title", "Bénéficiaire", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetOverrideAsync_InvalidatesCache()
    {
        ILocalizationOverrideStore inner = Substitute.For<ILocalizationOverrideStore>();

        int callCount = 0;
        inner.GetOverridesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(_ =>
        {
            callCount++;
            return Task.FromResult<IReadOnlyDictionary<string, string>>(
                new Dictionary<string, string> { ["Key"] = $"value{callCount}" });
        });
        inner.SetOverrideAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
             .ReturnsForAnyArgs(Task.CompletedTask);

        CachedLocalizationOverrideStore store = BuildStore(inner);

        // First call populates cache
        IReadOnlyDictionary<string, string> first =
            await store.GetOverridesAsync("TestApp", "fr", TestContext.Current.CancellationToken);

        // Write invalidates cache
        await store.SetOverrideAsync("TestApp", "fr", "Key", "updated",
            TestContext.Current.CancellationToken);

        // Second call should hit inner store again (cache was invalidated)
        IReadOnlyDictionary<string, string> second =
            await store.GetOverridesAsync("TestApp", "fr", TestContext.Current.CancellationToken);

        first["Key"].ShouldBe("value1");
        second["Key"].ShouldBe("value2");
        await inner.Received(2).GetOverridesAsync("TestApp", "fr", Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // RemoveOverrideAsync — forwards + invalidates cache
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RemoveOverrideAsync_ForwardsToInnerStore()
    {
        ILocalizationOverrideStore inner = Substitute.For<ILocalizationOverrideStore>();
        inner.RemoveOverrideAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
             .ReturnsForAnyArgs(Task.CompletedTask);

        CachedLocalizationOverrideStore store = BuildStore(inner);
        await store.RemoveOverrideAsync(
            "TestApp", "fr", "Patient.Title",
            TestContext.Current.CancellationToken);

        await inner.Received(1).RemoveOverrideAsync(
            "TestApp", "fr", "Patient.Title", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveOverrideAsync_InvalidatesCache()
    {
        ILocalizationOverrideStore inner = Substitute.For<ILocalizationOverrideStore>();

        int callCount = 0;
        inner.GetOverridesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(_ =>
        {
            callCount++;
            return Task.FromResult<IReadOnlyDictionary<string, string>>(
                new Dictionary<string, string>());
        });
        inner.RemoveOverrideAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
             .ReturnsForAnyArgs(Task.CompletedTask);

        CachedLocalizationOverrideStore store = BuildStore(inner);

        await store.GetOverridesAsync("TestApp", "fr", TestContext.Current.CancellationToken);
        await store.RemoveOverrideAsync("TestApp", "fr", "Key",
            TestContext.Current.CancellationToken);
        await store.GetOverridesAsync("TestApp", "fr", TestContext.Current.CancellationToken);

        await inner.Received(2).GetOverridesAsync("TestApp", "fr", Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // Cache key uniqueness (culture isolation)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetOverridesAsync_DifferentCultures_AreIsolatedInCache()
    {
        ILocalizationOverrideStore inner = Substitute.For<ILocalizationOverrideStore>();
        inner.GetOverridesAsync("TestApp", "fr", Arg.Any<CancellationToken>())
             .Returns(Task.FromResult<IReadOnlyDictionary<string, string>>(
                 new Dictionary<string, string> { ["Key"] = "Français" }));
        inner.GetOverridesAsync("TestApp", "en", Arg.Any<CancellationToken>())
             .Returns(Task.FromResult<IReadOnlyDictionary<string, string>>(
                 new Dictionary<string, string> { ["Key"] = "English" }));

        CachedLocalizationOverrideStore store = BuildStore(inner);

        IReadOnlyDictionary<string, string> fr =
            await store.GetOverridesAsync("TestApp", "fr", TestContext.Current.CancellationToken);
        IReadOnlyDictionary<string, string> en =
            await store.GetOverridesAsync("TestApp", "en", TestContext.Current.CancellationToken);

        fr["Key"].ShouldBe("Français");
        en["Key"].ShouldBe("English");
    }
}
