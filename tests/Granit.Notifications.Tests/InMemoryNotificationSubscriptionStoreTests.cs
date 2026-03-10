// =============================================================================
// Tests - InMemoryNotificationSubscriptionStore
// =============================================================================
// Verifies the in-memory subscription store: subscribe/unsubscribe, subscriber
// listing, entity follow/unfollow, follower queries, tenant isolation.
// =============================================================================

using Granit.Guids;
using Granit.Notifications.Domain;
using Granit.Notifications.Internal;
using Granit.Timing;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Tests;

public sealed class InMemoryNotificationSubscriptionStoreTests
{
    private readonly InMemoryNotificationSubscriptionStore _store;

    public InMemoryNotificationSubscriptionStoreTests()
    {
        IGuidGenerator guidGenerator = Substitute.For<IGuidGenerator>();
        guidGenerator.Create().Returns(_ => Guid.NewGuid());
        IClock clock = Substitute.For<IClock>();
        clock.Now.Returns(new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero));
        _store = new InMemoryNotificationSubscriptionStore(guidGenerator, clock);
    }

    // -------------------------------------------------------------------------
    // SubscribeAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SubscribeAsync_AddsSubscription()
    {
        await _store.SubscribeAsync("user-1", "order.created", tenantId: null, TestContext.Current.CancellationToken);

        IReadOnlyList<string> subscribers =
            await _store.GetSubscriberIdsAsync("order.created", tenantId: null, TestContext.Current.CancellationToken);

        subscribers.ShouldHaveSingleItem().ShouldBe("user-1");
    }

    [Fact]
    public async Task SubscribeAsync_Duplicate_DoesNotDuplicate()
    {
        await _store.SubscribeAsync("user-1", "order.created", tenantId: null, TestContext.Current.CancellationToken);
        await _store.SubscribeAsync("user-1", "order.created", tenantId: null, TestContext.Current.CancellationToken);

        IReadOnlyList<string> subscribers =
            await _store.GetSubscriberIdsAsync("order.created", tenantId: null, TestContext.Current.CancellationToken);

        subscribers.ShouldHaveSingleItem().ShouldBe("user-1");
    }

    // -------------------------------------------------------------------------
    // UnsubscribeAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UnsubscribeAsync_RemovesSubscription()
    {
        await _store.SubscribeAsync("user-1", "order.created", tenantId: null, TestContext.Current.CancellationToken);
        await _store.UnsubscribeAsync("user-1", "order.created", tenantId: null, TestContext.Current.CancellationToken);

        IReadOnlyList<string> subscribers =
            await _store.GetSubscriberIdsAsync("order.created", tenantId: null, TestContext.Current.CancellationToken);

        subscribers.ShouldBeEmpty();
    }

    [Fact]
    public async Task UnsubscribeAsync_NonExistent_DoesNotThrow()
    {
        Func<Task> act = () => _store.UnsubscribeAsync(
            "user-1", "order.created", tenantId: null, TestContext.Current.CancellationToken);

        await Should.NotThrowAsync(act);
    }

    // -------------------------------------------------------------------------
    // GetSubscriberIdsAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetSubscriberIdsAsync_ReturnsAllSubscribers()
    {
        await _store.SubscribeAsync("user-1", "order.created", tenantId: null, TestContext.Current.CancellationToken);
        await _store.SubscribeAsync("user-2", "order.created", tenantId: null, TestContext.Current.CancellationToken);
        await _store.SubscribeAsync("user-3", "order.shipped", tenantId: null, TestContext.Current.CancellationToken);

        IReadOnlyList<string> subscribers =
            await _store.GetSubscriberIdsAsync("order.created", tenantId: null, TestContext.Current.CancellationToken);

        subscribers.Count.ShouldBe(2);
        subscribers.ShouldContain("user-1");
        subscribers.ShouldContain("user-2");
    }

    [Fact]
    public async Task GetSubscriberIdsAsync_ExcludesEntityFollowers()
    {
        await _store.SubscribeAsync("user-1", "order.created", tenantId: null, TestContext.Current.CancellationToken);
        await _store.FollowEntityAsync("user-2", "Order", "order-42", tenantId: null, TestContext.Current.CancellationToken);

        IReadOnlyList<string> subscribers =
            await _store.GetSubscriberIdsAsync("order.created", tenantId: null, TestContext.Current.CancellationToken);

        subscribers.ShouldHaveSingleItem().ShouldBe("user-1");
    }

    [Fact]
    public async Task GetSubscriberIdsAsync_EmptyStore_ReturnsEmptyList()
    {
        IReadOnlyList<string> subscribers =
            await _store.GetSubscriberIdsAsync("order.created", tenantId: null, TestContext.Current.CancellationToken);

        subscribers.ShouldBeEmpty();
    }

    // -------------------------------------------------------------------------
    // GetUserSubscriptionsAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetUserSubscriptionsAsync_ReturnsAllUserSubscriptions()
    {
        await _store.SubscribeAsync("user-1", "order.created", tenantId: null, TestContext.Current.CancellationToken);
        await _store.SubscribeAsync("user-1", "order.shipped", tenantId: null, TestContext.Current.CancellationToken);
        await _store.SubscribeAsync("user-2", "order.created", tenantId: null, TestContext.Current.CancellationToken);

        IReadOnlyList<NotificationSubscription> subscriptions =
            await _store.GetUserSubscriptionsAsync("user-1", tenantId: null, TestContext.Current.CancellationToken);

        subscriptions.Count.ShouldBe(2);
        subscriptions.ShouldAllBe(s => s.UserId == "user-1");
    }

    // -------------------------------------------------------------------------
    // FollowEntityAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task FollowEntityAsync_AddsFollower()
    {
        await _store.FollowEntityAsync("user-1", "Order", "order-42", tenantId: null, TestContext.Current.CancellationToken);

        IReadOnlyList<string> followers =
            await _store.GetEntityFollowerIdsAsync("Order", "order-42", tenantId: null, TestContext.Current.CancellationToken);

        followers.ShouldHaveSingleItem().ShouldBe("user-1");
    }

    [Fact]
    public async Task FollowEntityAsync_Duplicate_DoesNotDuplicate()
    {
        await _store.FollowEntityAsync("user-1", "Order", "order-42", tenantId: null, TestContext.Current.CancellationToken);
        await _store.FollowEntityAsync("user-1", "Order", "order-42", tenantId: null, TestContext.Current.CancellationToken);

        IReadOnlyList<string> followers =
            await _store.GetEntityFollowerIdsAsync("Order", "order-42", tenantId: null, TestContext.Current.CancellationToken);

        followers.ShouldHaveSingleItem().ShouldBe("user-1");
    }

    // -------------------------------------------------------------------------
    // UnfollowEntityAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UnfollowEntityAsync_RemovesFollower()
    {
        await _store.FollowEntityAsync("user-1", "Order", "order-42", tenantId: null, TestContext.Current.CancellationToken);
        await _store.UnfollowEntityAsync("user-1", "Order", "order-42", tenantId: null, TestContext.Current.CancellationToken);

        IReadOnlyList<string> followers =
            await _store.GetEntityFollowerIdsAsync("Order", "order-42", tenantId: null, TestContext.Current.CancellationToken);

        followers.ShouldBeEmpty();
    }

    [Fact]
    public async Task UnfollowEntityAsync_NonExistent_DoesNotThrow()
    {
        Func<Task> act = () => _store.UnfollowEntityAsync(
            "user-1", "Order", "order-42", tenantId: null, TestContext.Current.CancellationToken);

        await Should.NotThrowAsync(act);
    }

    // -------------------------------------------------------------------------
    // GetEntityFollowerIdsAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetEntityFollowerIdsAsync_ReturnsAllFollowers()
    {
        await _store.FollowEntityAsync("user-1", "Order", "order-42", tenantId: null, TestContext.Current.CancellationToken);
        await _store.FollowEntityAsync("user-2", "Order", "order-42", tenantId: null, TestContext.Current.CancellationToken);
        await _store.FollowEntityAsync("user-3", "Order", "order-99", tenantId: null, TestContext.Current.CancellationToken);

        IReadOnlyList<string> followers =
            await _store.GetEntityFollowerIdsAsync("Order", "order-42", tenantId: null, TestContext.Current.CancellationToken);

        followers.Count.ShouldBe(2);
        followers.ShouldContain("user-1");
        followers.ShouldContain("user-2");
    }

    // -------------------------------------------------------------------------
    // GetEntityFollowersAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetEntityFollowersAsync_ReturnsFollowerDetails()
    {
        await _store.FollowEntityAsync("user-1", "Order", "order-42", tenantId: null, TestContext.Current.CancellationToken);
        await _store.FollowEntityAsync("user-2", "Order", "order-42", tenantId: null, TestContext.Current.CancellationToken);

        IReadOnlyList<NotificationSubscription> followers =
            await _store.GetEntityFollowersAsync("Order", "order-42", tenantId: null, TestContext.Current.CancellationToken);

        followers.Count.ShouldBe(2);
        followers.ShouldAllBe(s => s.EntityType == "Order" && s.EntityId == "order-42");
        followers.Select(f => f.UserId).ShouldContain("user-1");
        followers.Select(f => f.UserId).ShouldContain("user-2");
    }

    // -------------------------------------------------------------------------
    // IsFollowingEntityAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task IsFollowingEntityAsync_ReturnsTrue_WhenFollowing()
    {
        await _store.FollowEntityAsync("user-1", "Order", "order-42", tenantId: null, TestContext.Current.CancellationToken);

        bool isFollowing = await _store.IsFollowingEntityAsync(
            "user-1", "Order", "order-42", tenantId: null, TestContext.Current.CancellationToken);

        isFollowing.ShouldBeTrue();
    }

    [Fact]
    public async Task IsFollowingEntityAsync_ReturnsFalse_WhenNotFollowing()
    {
        bool isFollowing = await _store.IsFollowingEntityAsync(
            "user-1", "Order", "order-42", tenantId: null, TestContext.Current.CancellationToken);

        isFollowing.ShouldBeFalse();
    }

    // -------------------------------------------------------------------------
    // Tenant isolation
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetSubscriberIdsAsync_IsolatesByTenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await _store.SubscribeAsync("user-1", "order.created", tenantId: tenantA, TestContext.Current.CancellationToken);
        await _store.SubscribeAsync("user-2", "order.created", tenantId: tenantB, TestContext.Current.CancellationToken);

        IReadOnlyList<string> subscribersA =
            await _store.GetSubscriberIdsAsync("order.created", tenantId: tenantA, TestContext.Current.CancellationToken);
        IReadOnlyList<string> subscribersB =
            await _store.GetSubscriberIdsAsync("order.created", tenantId: tenantB, TestContext.Current.CancellationToken);

        subscribersA.ShouldHaveSingleItem().ShouldBe("user-1");
        subscribersB.ShouldHaveSingleItem().ShouldBe("user-2");
    }

    [Fact]
    public async Task GetEntityFollowerIdsAsync_IsolatesByTenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await _store.FollowEntityAsync("user-1", "Order", "order-42", tenantId: tenantA, TestContext.Current.CancellationToken);
        await _store.FollowEntityAsync("user-2", "Order", "order-42", tenantId: tenantB, TestContext.Current.CancellationToken);

        IReadOnlyList<string> followersA =
            await _store.GetEntityFollowerIdsAsync("Order", "order-42", tenantId: tenantA, TestContext.Current.CancellationToken);
        IReadOnlyList<string> followersB =
            await _store.GetEntityFollowerIdsAsync("Order", "order-42", tenantId: tenantB, TestContext.Current.CancellationToken);

        followersA.ShouldHaveSingleItem().ShouldBe("user-1");
        followersB.ShouldHaveSingleItem().ShouldBe("user-2");
    }

    [Fact]
    public async Task SubscribeAsync_SameTopic_DifferentTenants_BothStored()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await _store.SubscribeAsync("user-1", "order.created", tenantId: tenantA, TestContext.Current.CancellationToken);
        await _store.SubscribeAsync("user-1", "order.created", tenantId: tenantB, TestContext.Current.CancellationToken);

        IReadOnlyList<NotificationSubscription> subsA =
            await _store.GetUserSubscriptionsAsync("user-1", tenantId: tenantA, TestContext.Current.CancellationToken);
        IReadOnlyList<NotificationSubscription> subsB =
            await _store.GetUserSubscriptionsAsync("user-1", tenantId: tenantB, TestContext.Current.CancellationToken);

        subsA.ShouldHaveSingleItem();
        subsB.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task UnsubscribeAsync_OnlyAffectsCorrectTenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await _store.SubscribeAsync("user-1", "order.created", tenantId: tenantA, TestContext.Current.CancellationToken);
        await _store.SubscribeAsync("user-1", "order.created", tenantId: tenantB, TestContext.Current.CancellationToken);

        await _store.UnsubscribeAsync("user-1", "order.created", tenantId: tenantA, TestContext.Current.CancellationToken);

        IReadOnlyList<string> subscribersA =
            await _store.GetSubscriberIdsAsync("order.created", tenantId: tenantA, TestContext.Current.CancellationToken);
        IReadOnlyList<string> subscribersB =
            await _store.GetSubscriberIdsAsync("order.created", tenantId: tenantB, TestContext.Current.CancellationToken);

        subscribersA.ShouldBeEmpty("subscription was removed for tenant A");
        subscribersB.ShouldContain("user-1", "subscription for tenant B should not be affected");
    }
}
