// =============================================================================
// UserSettingValueProviderTests - Unit tests for the User provider
// =============================================================================
// Verifies the no-user guard, user-scoped reads/writes,
// and cache invalidation keyed by user ID.
// =============================================================================

using Granit.Caching;
using Granit.Security;
using Granit.Settings.Definitions;
using Granit.Settings.Options;
using Granit.Settings.Providers;
using Granit.Settings.Stores;
using Granit.Settings.Values;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.Settings.Tests;

public sealed class UserSettingValueProviderTests
{
    private const string UserId = "user-test-42";

    private static (UserSettingValueProvider provider, InMemorySettingStore store, ICacheService<SettingValue> cache)
        Create(bool authenticated = true, string? userId = UserId)
    {
        ICurrentUserService currentUser = Substitute.For<ICurrentUserService>();
        currentUser.IsAuthenticated.Returns(authenticated);
        currentUser.UserId.Returns(authenticated ? userId : null);

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
        UserSettingValueProvider provider = new(currentUser, store, store, cache, options);
        return (provider, store, cache);
    }

    [Fact]
    public void Name_Is_U() =>
        Create().provider.Name.ShouldBe("U");

    [Fact]
    public void Order_Is_100() =>
        Create().provider.Order.ShouldBe(100);

    [Fact]
    public async Task GetOrNullAsync_NotAuthenticated_Returns_Null()
    {
        (UserSettingValueProvider provider, _, _) = Create(authenticated: false);
        SettingDefinition def = new("App.Theme");

        SettingValue? result = await provider.GetOrNullAsync(def, TestContext.Current.CancellationToken);

        result.ShouldBeNull("unauthenticated user — provider must short-circuit");
    }

    [Fact]
    public async Task GetOrNullAsync_Authenticated_StoreHasValue_Returns_SettingValue()
    {
        (UserSettingValueProvider provider, InMemorySettingStore store, _) = Create();
        SettingDefinition def = new("App.Theme");
        await store.SetAsync("App.Theme", "U", UserId, "red", TestContext.Current.CancellationToken);

        SettingValue? result = await provider.GetOrNullAsync(def, TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result!.Value.ShouldBe("red");
        result.ProviderKey.ShouldBe(UserId);
    }

    [Fact]
    public async Task GetOrNullAsync_Authenticated_StoreEmpty_Returns_Null()
    {
        (UserSettingValueProvider provider, _, _) = Create();
        SettingDefinition def = new("App.Theme");

        SettingValue? result = await provider.GetOrNullAsync(def, TestContext.Current.CancellationToken);

        result.ShouldBeNull("sentinel with Value=null must be filtered out");
    }

    [Fact]
    public async Task SetAsync_NotAuthenticated_DoesNothing()
    {
        (UserSettingValueProvider provider, InMemorySettingStore store, _) = Create(authenticated: false);
        SettingDefinition def = new("App.Theme");

        await provider.SetAsync(def, "red", TestContext.Current.CancellationToken);

        IReadOnlyList<SettingValue> entries = await store.GetListAsync(
            "U", null, TestContext.Current.CancellationToken);
        entries.ShouldBeEmpty("unauthenticated — SetAsync must be a no-op");
    }

    [Fact]
    public async Task SetAsync_Authenticated_WritesToStore_WithUserId()
    {
        (UserSettingValueProvider provider, InMemorySettingStore store, ICacheService<SettingValue> cache) = Create();
        SettingDefinition def = new("App.Theme");

        await provider.SetAsync(def, "red", TestContext.Current.CancellationToken);

        SettingValue? stored = await store.GetOrNullAsync(
            "App.Theme", "U", UserId, TestContext.Current.CancellationToken);
        stored!.Value.ShouldBe("red");

        await cache.Received(1).RemoveAsync(
            Arg.Is<string>(k => k.Contains('U') && k.Contains(UserId) && k.Contains("App.Theme")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClearAsync_NotAuthenticated_DoesNothing()
    {
        (UserSettingValueProvider provider, _, _) = Create(authenticated: false);
        SettingDefinition def = new("App.Theme");

        Func<Task> act = () => provider.ClearAsync(def, TestContext.Current.CancellationToken);

        await Should.NotThrowAsync(act);
    }

    [Fact]
    public async Task ClearAsync_Authenticated_DeletesFromStore_And_InvalidatesCache()
    {
        (UserSettingValueProvider provider, InMemorySettingStore store, ICacheService<SettingValue> cache) = Create();
        SettingDefinition def = new("App.Theme");
        await store.SetAsync("App.Theme", "U", UserId, "red", TestContext.Current.CancellationToken);

        await provider.ClearAsync(def, TestContext.Current.CancellationToken);

        SettingValue? stored = await store.GetOrNullAsync(
            "App.Theme", "U", UserId, TestContext.Current.CancellationToken);
        stored.ShouldBeNull();

        await cache.Received(1).RemoveAsync(
            Arg.Is<string>(k => k.Contains('U') && k.Contains(UserId) && k.Contains("App.Theme")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetOrNullAsync_Isolates_Entries_By_UserId()
    {
        (UserSettingValueProvider providerA, InMemorySettingStore store, _) = Create(userId: "user-A");
        await store.SetAsync("App.Theme", "U", "user-A", "red", TestContext.Current.CancellationToken);
        await store.SetAsync("App.Theme", "U", "user-B", "blue", TestContext.Current.CancellationToken);

        SettingValue? result = await providerA.GetOrNullAsync(
            new SettingDefinition("App.Theme"), TestContext.Current.CancellationToken);

        result!.Value.ShouldBe("red", "provider must only read user-A's value");
    }
}
