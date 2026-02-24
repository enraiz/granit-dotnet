// =============================================================================
// TenantSettingValueProviderTests - Unit tests for the Tenant provider
// =============================================================================
// Verifies the no-tenant guard, tenant-scoped reads/writes,
// and cache invalidation keyed by tenant ID.
// =============================================================================

using FluentAssertions;
using Granit.Caching;
using Granit.Core.MultiTenancy;
using Granit.Settings.Definitions;
using Granit.Settings.Options;
using Granit.Settings.Providers;
using Granit.Settings.Stores;
using Granit.Settings.Values;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Granit.Settings.Tests;

public sealed class TenantSettingValueProviderTests
{
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static (TenantSettingValueProvider provider, InMemorySettingStore store, ICacheService<SettingValue> cache)
        Create(bool tenantAvailable = true, Guid? tenantId = null)
    {
        ICurrentTenant currentTenant = Substitute.For<ICurrentTenant>();
        currentTenant.IsAvailable.Returns(tenantAvailable);
        currentTenant.Id.Returns(tenantAvailable ? (tenantId ?? TenantId) : null);

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
        TenantSettingValueProvider provider = new(currentTenant, store, cache, options);
        return (provider, store, cache);
    }

    [Fact]
    public void Name_Is_T() =>
        Create().provider.Name.Should().Be("T");

    [Fact]
    public void Order_Is_200() =>
        Create().provider.Order.Should().Be(200);

    [Fact]
    public async Task GetOrNullAsync_NoTenant_Returns_Null()
    {
        (TenantSettingValueProvider provider, _, _) = Create(tenantAvailable: false);
        SettingDefinition def = new("App.Theme");

        SettingValue? result = await provider.GetOrNullAsync(def, TestContext.Current.CancellationToken);

        result.Should().BeNull("no active tenant — provider must short-circuit");
    }

    [Fact]
    public async Task GetOrNullAsync_TenantActive_StoreHasValue_Returns_SettingValue()
    {
        (TenantSettingValueProvider provider, InMemorySettingStore store, _) = Create();
        SettingDefinition def = new("App.Theme");
        await store.SetAsync("App.Theme", "T", TenantId.ToString(), "blue", TestContext.Current.CancellationToken);

        SettingValue? result = await provider.GetOrNullAsync(def, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Value.Should().Be("blue");
        result.ProviderKey.Should().Be(TenantId.ToString());
    }

    [Fact]
    public async Task GetOrNullAsync_TenantActive_StoreEmpty_Returns_Null()
    {
        (TenantSettingValueProvider provider, _, _) = Create();
        SettingDefinition def = new("App.Theme");

        SettingValue? result = await provider.GetOrNullAsync(def, TestContext.Current.CancellationToken);

        result.Should().BeNull("sentinel with Value=null must be filtered out");
    }

    [Fact]
    public async Task SetAsync_NoTenant_DoesNothing()
    {
        (TenantSettingValueProvider provider, InMemorySettingStore store, _) = Create(tenantAvailable: false);
        SettingDefinition def = new("App.Theme");

        await provider.SetAsync(def, "blue", TestContext.Current.CancellationToken);

        IReadOnlyList<SettingValue> entries = await store.GetListAsync(
            "T", null, TestContext.Current.CancellationToken);
        entries.Should().BeEmpty("no tenant — SetAsync must be a no-op");
    }

    [Fact]
    public async Task SetAsync_TenantActive_WritesToStore_WithTenantKey()
    {
        (TenantSettingValueProvider provider, InMemorySettingStore store, ICacheService<SettingValue> cache) = Create();
        SettingDefinition def = new("App.Theme");

        await provider.SetAsync(def, "blue", TestContext.Current.CancellationToken);

        SettingValue? stored = await store.GetOrNullAsync(
            "App.Theme", "T", TenantId.ToString(), TestContext.Current.CancellationToken);
        stored!.Value.Should().Be("blue");

        await cache.Received(1).RemoveAsync(
            Arg.Is<string>(k => k.Contains('T') && k.Contains(TenantId.ToString()) && k.Contains("App.Theme")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClearAsync_NoTenant_DoesNothing()
    {
        (TenantSettingValueProvider provider, InMemorySettingStore store, _) = Create(tenantAvailable: false);
        SettingDefinition def = new("App.Theme");

        Func<Task> act = () => provider.ClearAsync(def, TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ClearAsync_TenantActive_DeletesFromStore_And_InvalidatesCache()
    {
        (TenantSettingValueProvider provider, InMemorySettingStore store, ICacheService<SettingValue> cache) = Create();
        SettingDefinition def = new("App.Theme");
        await store.SetAsync("App.Theme", "T", TenantId.ToString(), "blue", TestContext.Current.CancellationToken);

        await provider.ClearAsync(def, TestContext.Current.CancellationToken);

        SettingValue? stored = await store.GetOrNullAsync(
            "App.Theme", "T", TenantId.ToString(), TestContext.Current.CancellationToken);
        stored.Should().BeNull();

        await cache.Received(1).RemoveAsync(
            Arg.Is<string>(k => k.Contains('T') && k.Contains(TenantId.ToString()) && k.Contains("App.Theme")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetOrNullAsync_Isolates_Entries_By_TenantId()
    {
        var tenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var tenantB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        (TenantSettingValueProvider providerA, InMemorySettingStore store, _) = Create(tenantId: tenantA);
        await store.SetAsync("App.Theme", "T", tenantA.ToString(), "blue", TestContext.Current.CancellationToken);
        await store.SetAsync("App.Theme", "T", tenantB.ToString(), "red", TestContext.Current.CancellationToken);

        SettingValue? result = await providerA.GetOrNullAsync(
            new SettingDefinition("App.Theme"), TestContext.Current.CancellationToken);

        result!.Value.Should().Be("blue", "provider must only see tenant A's value");
    }
}
