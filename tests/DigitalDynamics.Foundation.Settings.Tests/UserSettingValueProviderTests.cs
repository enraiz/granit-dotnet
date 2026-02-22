// =============================================================================
// UserSettingValueProviderTests - Unit tests for the User provider
// =============================================================================
// Verifies the no-user guard, user-scoped reads/writes,
// and cache invalidation keyed by user ID.
// =============================================================================

using DigitalDynamics.Foundation.Caching;
using DigitalDynamics.Foundation.Security;
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
        UserSettingValueProvider provider = new(currentUser, store, cache, options);
        return (provider, store, cache);
    }

    [Fact]
    public void Name_Is_U() =>
        Create().provider.Name.Should().Be("U");

    [Fact]
    public void Order_Is_100() =>
        Create().provider.Order.Should().Be(100);

    [Fact]
    public async Task GetOrNullAsync_NotAuthenticated_Returns_Null()
    {
        (UserSettingValueProvider provider, _, _) = Create(authenticated: false);
        SettingDefinition def = new("App.Theme");

        SettingValue? result = await provider.GetOrNullAsync(def, TestContext.Current.CancellationToken);

        result.Should().BeNull("unauthenticated user — provider must short-circuit");
    }

    [Fact]
    public async Task GetOrNullAsync_Authenticated_StoreHasValue_Returns_SettingValue()
    {
        (UserSettingValueProvider provider, InMemorySettingStore store, _) = Create();
        SettingDefinition def = new("App.Theme");
        await store.SetAsync("App.Theme", "U", UserId, "red", TestContext.Current.CancellationToken);

        SettingValue? result = await provider.GetOrNullAsync(def, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Value.Should().Be("red");
        result.ProviderKey.Should().Be(UserId);
    }

    [Fact]
    public async Task GetOrNullAsync_Authenticated_StoreEmpty_Returns_Null()
    {
        (UserSettingValueProvider provider, _, _) = Create();
        SettingDefinition def = new("App.Theme");

        SettingValue? result = await provider.GetOrNullAsync(def, TestContext.Current.CancellationToken);

        result.Should().BeNull("sentinel with Value=null must be filtered out");
    }

    [Fact]
    public async Task SetAsync_NotAuthenticated_DoesNothing()
    {
        (UserSettingValueProvider provider, InMemorySettingStore store, _) = Create(authenticated: false);
        SettingDefinition def = new("App.Theme");

        await provider.SetAsync(def, "red", TestContext.Current.CancellationToken);

        IReadOnlyList<SettingValue> entries = await store.GetListAsync(
            "U", null, TestContext.Current.CancellationToken);
        entries.Should().BeEmpty("unauthenticated — SetAsync must be a no-op");
    }

    [Fact]
    public async Task SetAsync_Authenticated_WritesToStore_WithUserId()
    {
        (UserSettingValueProvider provider, InMemorySettingStore store, ICacheService<SettingValue> cache) = Create();
        SettingDefinition def = new("App.Theme");

        await provider.SetAsync(def, "red", TestContext.Current.CancellationToken);

        SettingValue? stored = await store.GetOrNullAsync(
            "App.Theme", "U", UserId, TestContext.Current.CancellationToken);
        stored!.Value.Should().Be("red");

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

        await act.Should().NotThrowAsync();
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
        stored.Should().BeNull();

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

        result!.Value.Should().Be("red", "provider must only read user-A's value");
    }
}
