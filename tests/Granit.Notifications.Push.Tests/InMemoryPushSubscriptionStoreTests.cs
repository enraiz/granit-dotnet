// =============================================================================
// Tests - InMemoryPushSubscriptionStore
// =============================================================================
// Verifies the in-memory push subscription store: CRUD operations, endpoint
// deduplication (upsert), tenant isolation, and multi-subscription support.
// =============================================================================

using FluentAssertions;
using Xunit;

namespace Granit.Notifications.Push.Tests;

public sealed class InMemoryPushSubscriptionStoreTests
{
    private readonly InMemoryPushSubscriptionStore _store = new();

    [Fact]
    public async Task SaveAndGet_ReturnsSavedSubscription()
    {
        PushSubscriptionInfo subscription = BuildSubscription("https://push.example.com/1");

        await _store.SaveSubscriptionAsync("user-1", subscription, null, TestContext.Current.CancellationToken);
        IReadOnlyList<PushSubscriptionInfo> result = await _store.GetSubscriptionsAsync(
            "user-1", null, TestContext.Current.CancellationToken);

        result.Should().ContainSingle()
            .Which.Endpoint.Should().Be("https://push.example.com/1");
    }

    [Fact]
    public async Task Save_SameEndpoint_UpdatesSubscription()
    {
        PushSubscriptionInfo original = BuildSubscription("https://push.example.com/1", auth: "auth-old");
        PushSubscriptionInfo updated = BuildSubscription("https://push.example.com/1", auth: "auth-new");

        await _store.SaveSubscriptionAsync("user-1", original, null, TestContext.Current.CancellationToken);
        await _store.SaveSubscriptionAsync("user-1", updated, null, TestContext.Current.CancellationToken);
        IReadOnlyList<PushSubscriptionInfo> result = await _store.GetSubscriptionsAsync(
            "user-1", null, TestContext.Current.CancellationToken);

        result.Should().ContainSingle()
            .Which.Auth.Should().Be("auth-new");
    }

    [Fact]
    public async Task Remove_DeletesSubscription()
    {
        PushSubscriptionInfo subscription = BuildSubscription("https://push.example.com/1");
        await _store.SaveSubscriptionAsync("user-1", subscription, null, TestContext.Current.CancellationToken);

        await _store.RemoveSubscriptionAsync("https://push.example.com/1", null, TestContext.Current.CancellationToken);
        IReadOnlyList<PushSubscriptionInfo> result = await _store.GetSubscriptionsAsync(
            "user-1", null, TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_EmptyStore_ReturnsEmpty()
    {
        IReadOnlyList<PushSubscriptionInfo> result = await _store.GetSubscriptionsAsync(
            "user-1", null, TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_DifferentTenant_ReturnsEmpty()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();
        PushSubscriptionInfo subscription = BuildSubscription("https://push.example.com/1");
        await _store.SaveSubscriptionAsync("user-1", subscription, tenantA, TestContext.Current.CancellationToken);

        IReadOnlyList<PushSubscriptionInfo> result = await _store.GetSubscriptionsAsync(
            "user-1", tenantB, TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Save_MultipleSubscriptions_ReturnsAll()
    {
        PushSubscriptionInfo sub1 = BuildSubscription("https://push.example.com/1");
        PushSubscriptionInfo sub2 = BuildSubscription("https://push.example.com/2");
        await _store.SaveSubscriptionAsync("user-1", sub1, null, TestContext.Current.CancellationToken);
        await _store.SaveSubscriptionAsync("user-1", sub2, null, TestContext.Current.CancellationToken);

        IReadOnlyList<PushSubscriptionInfo> result = await _store.GetSubscriptionsAsync(
            "user-1", null, TestContext.Current.CancellationToken);

        result.Should().HaveCount(2);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static PushSubscriptionInfo BuildSubscription(string endpoint, string auth = "auth-key") => new()
    {
        Endpoint = endpoint,
        P256dh = "p256dh-key",
        Auth = auth,
    };
}
