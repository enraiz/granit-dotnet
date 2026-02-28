// =============================================================================
// Tests - InMemoryNotificationSubscriptionStore
// =============================================================================
// Verifies the in-memory subscription store: subscribe/unsubscribe, subscriber
// listing, entity follow/unfollow, follower queries, tenant isolation.
// =============================================================================

using FluentAssertions;
using Granit.Notifications.Domain;
using Granit.Notifications.Internal;
using Xunit;

namespace Granit.Notifications.Tests;

public sealed class InMemoryNotificationSubscriptionStoreTests
{
    private readonly InMemoryNotificationSubscriptionStore _store = new();

    // -------------------------------------------------------------------------
    // SubscribeAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SubscribeAsync_AddsSubscription()
    {
        await _store.SubscribeAsync("user-1", "order.created", tenantId: null, TestContext.Current.CancellationToken);

        IReadOnlyList<string> subscribers =
            await _store.GetSubscriberIdsAsync("order.created", tenantId: null, TestContext.Current.CancellationToken);

        subscribers.Should().ContainSingle().Which.Should().Be("user-1");
    }

    [Fact]
    public async Task SubscribeAsync_Duplicate_DoesNotDuplicate()
    {
        await _store.SubscribeAsync("user-1", "order.created", tenantId: null, TestContext.Current.CancellationToken);
        await _store.SubscribeAsync("user-1", "order.created", tenantId: null, TestContext.Current.CancellationToken);

        IReadOnlyList<string> subscribers =
            await _store.GetSubscriberIdsAsync("order.created", tenantId: null, TestContext.Current.CancellationToken);

        subscribers.Should().ContainSingle("subscribing twice should not create duplicates");
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

        subscribers.Should().BeEmpty();
    }

    [Fact]
    public async Task UnsubscribeAsync_NonExistent_DoesNotThrow()
    {
        Func<Task> act = () => _store.UnsubscribeAsync(
            "user-1", "order.created", tenantId: null, TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
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

        subscribers.Should().HaveCount(2);
        subscribers.Should().Contain("user-1");
        subscribers.Should().Contain("user-2");
    }

    [Fact]
    public async Task GetSubscriberIdsAsync_ExcludesEntityFollowers()
    {
        await _store.SubscribeAsync("user-1", "order.created", tenantId: null, TestContext.Current.CancellationToken);
        await _store.FollowEntityAsync("user-2", "Order", "order-42", tenantId: null, TestContext.Current.CancellationToken);

        IReadOnlyList<string> subscribers =
            await _store.GetSubscriberIdsAsync("order.created", tenantId: null, TestContext.Current.CancellationToken);

        subscribers.Should().ContainSingle().Which.Should().Be("user-1");
    }

    [Fact]
    public async Task GetSubscriberIdsAsync_EmptyStore_ReturnsEmptyList()
    {
        IReadOnlyList<string> subscribers =
            await _store.GetSubscriberIdsAsync("order.created", tenantId: null, TestContext.Current.CancellationToken);

        subscribers.Should().BeEmpty();
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

        subscriptions.Should().HaveCount(2);
        subscriptions.Should().OnlyContain(s => s.UserId == "user-1");
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

        followers.Should().ContainSingle().Which.Should().Be("user-1");
    }

    [Fact]
    public async Task FollowEntityAsync_Duplicate_DoesNotDuplicate()
    {
        await _store.FollowEntityAsync("user-1", "Order", "order-42", tenantId: null, TestContext.Current.CancellationToken);
        await _store.FollowEntityAsync("user-1", "Order", "order-42", tenantId: null, TestContext.Current.CancellationToken);

        IReadOnlyList<string> followers =
            await _store.GetEntityFollowerIdsAsync("Order", "order-42", tenantId: null, TestContext.Current.CancellationToken);

        followers.Should().ContainSingle("following twice should not create duplicates");
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

        followers.Should().BeEmpty();
    }

    [Fact]
    public async Task UnfollowEntityAsync_NonExistent_DoesNotThrow()
    {
        Func<Task> act = () => _store.UnfollowEntityAsync(
            "user-1", "Order", "order-42", tenantId: null, TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
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

        followers.Should().HaveCount(2);
        followers.Should().Contain("user-1");
        followers.Should().Contain("user-2");
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

        followers.Should().HaveCount(2);
        followers.Should().OnlyContain(s => s.EntityType == "Order" && s.EntityId == "order-42");
        followers.Select(f => f.UserId).Should().Contain("user-1").And.Contain("user-2");
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

        isFollowing.Should().BeTrue();
    }

    [Fact]
    public async Task IsFollowingEntityAsync_ReturnsFalse_WhenNotFollowing()
    {
        bool isFollowing = await _store.IsFollowingEntityAsync(
            "user-1", "Order", "order-42", tenantId: null, TestContext.Current.CancellationToken);

        isFollowing.Should().BeFalse();
    }

    // -------------------------------------------------------------------------
    // Tenant isolation
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetSubscriberIdsAsync_IsolatesByTenant()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();

        await _store.SubscribeAsync("user-1", "order.created", tenantId: tenantA, TestContext.Current.CancellationToken);
        await _store.SubscribeAsync("user-2", "order.created", tenantId: tenantB, TestContext.Current.CancellationToken);

        IReadOnlyList<string> subscribersA =
            await _store.GetSubscriberIdsAsync("order.created", tenantId: tenantA, TestContext.Current.CancellationToken);
        IReadOnlyList<string> subscribersB =
            await _store.GetSubscriberIdsAsync("order.created", tenantId: tenantB, TestContext.Current.CancellationToken);

        subscribersA.Should().ContainSingle().Which.Should().Be("user-1");
        subscribersB.Should().ContainSingle().Which.Should().Be("user-2");
    }

    [Fact]
    public async Task GetEntityFollowerIdsAsync_IsolatesByTenant()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();

        await _store.FollowEntityAsync("user-1", "Order", "order-42", tenantId: tenantA, TestContext.Current.CancellationToken);
        await _store.FollowEntityAsync("user-2", "Order", "order-42", tenantId: tenantB, TestContext.Current.CancellationToken);

        IReadOnlyList<string> followersA =
            await _store.GetEntityFollowerIdsAsync("Order", "order-42", tenantId: tenantA, TestContext.Current.CancellationToken);
        IReadOnlyList<string> followersB =
            await _store.GetEntityFollowerIdsAsync("Order", "order-42", tenantId: tenantB, TestContext.Current.CancellationToken);

        followersA.Should().ContainSingle().Which.Should().Be("user-1");
        followersB.Should().ContainSingle().Which.Should().Be("user-2");
    }

    [Fact]
    public async Task SubscribeAsync_SameTopic_DifferentTenants_BothStored()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();

        await _store.SubscribeAsync("user-1", "order.created", tenantId: tenantA, TestContext.Current.CancellationToken);
        await _store.SubscribeAsync("user-1", "order.created", tenantId: tenantB, TestContext.Current.CancellationToken);

        IReadOnlyList<NotificationSubscription> subsA =
            await _store.GetUserSubscriptionsAsync("user-1", tenantId: tenantA, TestContext.Current.CancellationToken);
        IReadOnlyList<NotificationSubscription> subsB =
            await _store.GetUserSubscriptionsAsync("user-1", tenantId: tenantB, TestContext.Current.CancellationToken);

        subsA.Should().ContainSingle();
        subsB.Should().ContainSingle();
    }

    [Fact]
    public async Task UnsubscribeAsync_OnlyAffectsCorrectTenant()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();

        await _store.SubscribeAsync("user-1", "order.created", tenantId: tenantA, TestContext.Current.CancellationToken);
        await _store.SubscribeAsync("user-1", "order.created", tenantId: tenantB, TestContext.Current.CancellationToken);

        await _store.UnsubscribeAsync("user-1", "order.created", tenantId: tenantA, TestContext.Current.CancellationToken);

        IReadOnlyList<string> subscribersA =
            await _store.GetSubscriberIdsAsync("order.created", tenantId: tenantA, TestContext.Current.CancellationToken);
        IReadOnlyList<string> subscribersB =
            await _store.GetSubscriberIdsAsync("order.created", tenantId: tenantB, TestContext.Current.CancellationToken);

        subscribersA.Should().BeEmpty("subscription was removed for tenant A");
        subscribersB.Should().ContainSingle("subscription for tenant B should not be affected");
    }
}
