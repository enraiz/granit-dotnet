// =============================================================================
// GlobalSettingValueProviderTests - Unit tests for the Global provider
// =============================================================================
// Verifies cache pass-through for reads, cache invalidation on Set/Clear,
// and the sentinel pattern (Value=null → provider returns null).
// =============================================================================

using DigitalDynamics.Foundation.Caching;
using DigitalDynamics.Foundation.Settings.Definitions;
using DigitalDynamics.Foundation.Settings.Options;
using DigitalDynamics.Foundation.Settings.Providers;
using DigitalDynamics.Foundation.Settings.Stores;
using DigitalDynamics.Foundation.Settings.Values;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace DigitalDynamics.Foundation.Settings.Tests;

public sealed class GlobalSettingValueProviderTests
{
    private static (GlobalSettingValueProvider provider, InMemorySettingStore store, ICacheService<SettingValue> cache)
        Create()
    {
        InMemorySettingStore store = new();
        ICacheService<SettingValue> cache = Substitute.For<ICacheService<SettingValue>>();
        // Simulate cache miss: invoke the factory directly
        cache.GetOrAddAsync(
                Arg.Any<string>(),
                Arg.Any<Func<CancellationToken, Task<SettingValue>>>(),
                Arg.Any<DistributedCacheEntryOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
                callInfo.ArgAt<Func<CancellationToken, Task<SettingValue>>>(1)(CancellationToken.None));

        IOptions<SettingsOptions> options = Microsoft.Extensions.Options.Options.Create(new SettingsOptions());
        GlobalSettingValueProvider provider = new(store, cache, options);
        return (provider, store, cache);
    }

    [Fact]
    public void Name_Is_G() =>
        Create().provider.Name.Should().Be("G");

    [Fact]
    public void Order_Is_300() =>
        Create().provider.Order.Should().Be(300);

    [Fact]
    public async Task GetOrNullAsync_StoreHasValue_Returns_SettingValue()
    {
        (GlobalSettingValueProvider provider, InMemorySettingStore store, _) = Create();
        SettingDefinition def = new("App.Theme");
        await store.SetAsync("App.Theme", "G", null, "dark", TestContext.Current.CancellationToken);

        SettingValue? result = await provider.GetOrNullAsync(def, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Value.Should().Be("dark");
        result.ProviderName.Should().Be("G");
        result.ProviderKey.Should().BeNull();
    }

    [Fact]
    public async Task GetOrNullAsync_StoreEmpty_Returns_Null()
    {
        (GlobalSettingValueProvider provider, _, _) = Create();
        SettingDefinition def = new("App.Theme");

        SettingValue? result = await provider.GetOrNullAsync(def, TestContext.Current.CancellationToken);

        result.Should().BeNull("sentinel with Value=null must be filtered out");
    }

    [Fact]
    public async Task SetAsync_WritesToStore_And_InvalidatesCache()
    {
        (GlobalSettingValueProvider provider, InMemorySettingStore store, ICacheService<SettingValue> cache) = Create();
        SettingDefinition def = new("App.Theme");

        await provider.SetAsync(def, "light", TestContext.Current.CancellationToken);

        SettingValue? stored = await store.GetOrNullAsync(
            "App.Theme", "G", null, TestContext.Current.CancellationToken);
        stored!.Value.Should().Be("light");

        await cache.Received(1).RemoveAsync(
            Arg.Is<string>(k => k.Contains('G') && k.Contains("App.Theme")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClearAsync_DeletesFromStore_And_InvalidatesCache()
    {
        (GlobalSettingValueProvider provider, InMemorySettingStore store, ICacheService<SettingValue> cache) = Create();
        SettingDefinition def = new("App.Theme");
        await store.SetAsync("App.Theme", "G", null, "dark", TestContext.Current.CancellationToken);

        await provider.ClearAsync(def, TestContext.Current.CancellationToken);

        SettingValue? stored = await store.GetOrNullAsync(
            "App.Theme", "G", null, TestContext.Current.CancellationToken);
        stored.Should().BeNull("ClearAsync must delete the store entry");

        await cache.Received(1).RemoveAsync(
            Arg.Is<string>(k => k.Contains('G') && k.Contains("App.Theme")),
            Arg.Any<CancellationToken>());
    }
}
