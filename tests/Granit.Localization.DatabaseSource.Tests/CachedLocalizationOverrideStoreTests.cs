using FluentAssertions;
using Granit.Localization;
using Granit.Localization.DatabaseSource;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Granit.Localization.DatabaseSource.Tests;

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
        IOptions<LocalizationDatabaseSourceOptions> options = Options.Create(
            new LocalizationDatabaseSourceOptions { CacheTtl = cacheTtl ?? TimeSpan.FromMinutes(5) });

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
        inner.GetOverridesAsync("Guava", "fr", Arg.Any<CancellationToken>())
             .ReturnsForAnyArgs(Task.FromResult<IReadOnlyDictionary<string, string>>(
                 new Dictionary<string, string> { ["Key1"] = "Valeur1" }));

        CachedLocalizationOverrideStore store = BuildStore(inner);
        IReadOnlyDictionary<string, string> result =
            await store.GetOverridesAsync("Guava", "fr", TestContext.Current.CancellationToken);

        result["Key1"].Should().Be("Valeur1");
        await inner.Received(1).GetOverridesAsync("Guava", "fr", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetOverridesAsync_CacheHit_DoesNotCallInnerStoreAgain()
    {
        ILocalizationOverrideStore inner = Substitute.For<ILocalizationOverrideStore>();
        inner.GetOverridesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
             .ReturnsForAnyArgs(Task.FromResult<IReadOnlyDictionary<string, string>>(
                 new Dictionary<string, string> { ["Key1"] = "Valeur1" }));

        CachedLocalizationOverrideStore store = BuildStore(inner);

        await store.GetOverridesAsync("Guava", "fr", TestContext.Current.CancellationToken);
        await store.GetOverridesAsync("Guava", "fr", TestContext.Current.CancellationToken);

        await inner.Received(1).GetOverridesAsync("Guava", "fr", Arg.Any<CancellationToken>());
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
            "Guava", "fr", "Patient.Title", "Bénéficiaire",
            TestContext.Current.CancellationToken);

        await inner.Received(1).SetOverrideAsync(
            "Guava", "fr", "Patient.Title", "Bénéficiaire", Arg.Any<CancellationToken>());
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
            await store.GetOverridesAsync("Guava", "fr", TestContext.Current.CancellationToken);

        // Write invalidates cache
        await store.SetOverrideAsync("Guava", "fr", "Key", "updated",
            TestContext.Current.CancellationToken);

        // Second call should hit inner store again (cache was invalidated)
        IReadOnlyDictionary<string, string> second =
            await store.GetOverridesAsync("Guava", "fr", TestContext.Current.CancellationToken);

        first["Key"].Should().Be("value1");
        second["Key"].Should().Be("value2");
        await inner.Received(2).GetOverridesAsync("Guava", "fr", Arg.Any<CancellationToken>());
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
            "Guava", "fr", "Patient.Title",
            TestContext.Current.CancellationToken);

        await inner.Received(1).RemoveOverrideAsync(
            "Guava", "fr", "Patient.Title", Arg.Any<CancellationToken>());
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

        await store.GetOverridesAsync("Guava", "fr", TestContext.Current.CancellationToken);
        await store.RemoveOverrideAsync("Guava", "fr", "Key",
            TestContext.Current.CancellationToken);
        await store.GetOverridesAsync("Guava", "fr", TestContext.Current.CancellationToken);

        await inner.Received(2).GetOverridesAsync("Guava", "fr", Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // Cache key uniqueness (culture isolation)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetOverridesAsync_DifferentCultures_AreIsolatedInCache()
    {
        ILocalizationOverrideStore inner = Substitute.For<ILocalizationOverrideStore>();
        inner.GetOverridesAsync("Guava", "fr", Arg.Any<CancellationToken>())
             .Returns(Task.FromResult<IReadOnlyDictionary<string, string>>(
                 new Dictionary<string, string> { ["Key"] = "Français" }));
        inner.GetOverridesAsync("Guava", "en", Arg.Any<CancellationToken>())
             .Returns(Task.FromResult<IReadOnlyDictionary<string, string>>(
                 new Dictionary<string, string> { ["Key"] = "English" }));

        CachedLocalizationOverrideStore store = BuildStore(inner);

        IReadOnlyDictionary<string, string> fr =
            await store.GetOverridesAsync("Guava", "fr", TestContext.Current.CancellationToken);
        IReadOnlyDictionary<string, string> en =
            await store.GetOverridesAsync("Guava", "en", TestContext.Current.CancellationToken);

        fr["Key"].Should().Be("Français");
        en["Key"].Should().Be("English");
    }
}
