// =============================================================================
// Tests - EfCoreUserNotificationStore
// =============================================================================
// Verifies CRUD operations on in-app user notifications (inbox):
// insert, get, paginated list, unread count, mark as read, entity feed.
// =============================================================================

using System.Text.Json;
using FluentAssertions;
using Granit.Notifications.Domain;
using Xunit;

namespace Granit.Notifications.EntityFrameworkCore.Tests;

public sealed class EfCoreUserNotificationStoreTests : IDisposable
{
    private readonly TestDbContextFactory _factory = TestDbContextFactory.Create();
    private readonly EfCoreUserNotificationStore _store;

    public EfCoreUserNotificationStoreTests()
    {
        _store = new EfCoreUserNotificationStore(_factory);
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task InsertAsync_ThenGetAsync_ReturnsNotification()
    {
        UserNotification notification = BuildNotification();

        await _store.InsertAsync(notification, TestContext.Current.CancellationToken);

        UserNotification? result = await _store.GetAsync(notification.Id, TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!.Id.Should().Be(notification.Id);
        result.RecipientUserId.Should().Be(notification.RecipientUserId);
        result.NotificationTypeName.Should().Be(notification.NotificationTypeName);
        result.State.Should().Be(UserNotificationState.Unread);
    }

    [Fact]
    public async Task GetListAsync_ReturnsPaginatedResults_SortedByDate()
    {
        string userId = "user-paginated";
        Guid tenantId = Guid.NewGuid();
        DateTimeOffset baseTime = DateTimeOffset.UtcNow;

        // Insert 5 notifications with different timestamps
        for (int i = 0; i < 5; i++)
        {
            UserNotification notification = BuildNotification(recipientUserId: userId, tenantId: tenantId, createdAt: baseTime.AddMinutes(i));
            await _store.InsertAsync(notification, TestContext.Current.CancellationToken);
        }

        // Request page of 3, skipping 1
        IReadOnlyList<UserNotification> result = await _store.GetListAsync(userId, tenantId, skipCount: 1, maxResultCount: 3, TestContext.Current.CancellationToken);

        result.Should().HaveCount(3);
        // Should be sorted descending by CreatedAt, so after skip 1 we get items at index 3, 2, 1
        result[0].CreatedAt.Should().BeOnOrAfter(result[1].CreatedAt);
        result[1].CreatedAt.Should().BeOnOrAfter(result[2].CreatedAt);
    }

    [Fact]
    public async Task GetListAsync_FiltersByRecipientAndTenant()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();

        await _store.InsertAsync(BuildNotification(recipientUserId: "user-a", tenantId: tenantA), TestContext.Current.CancellationToken);
        await _store.InsertAsync(BuildNotification(recipientUserId: "user-a", tenantId: tenantB), TestContext.Current.CancellationToken);
        await _store.InsertAsync(BuildNotification(recipientUserId: "user-b", tenantId: tenantA), TestContext.Current.CancellationToken);

        IReadOnlyList<UserNotification> result = await _store.GetListAsync("user-a", tenantA, skipCount: 0, maxResultCount: 100, TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        result[0].RecipientUserId.Should().Be("user-a");
        result[0].TenantId.Should().Be(tenantA);
    }

    [Fact]
    public async Task GetUnreadCountAsync_ReturnsCorrectCount()
    {
        string userId = "user-unread-count";
        Guid tenantId = Guid.NewGuid();

        // Insert 3 unread notifications
        for (int i = 0; i < 3; i++)
        {
            await _store.InsertAsync(BuildNotification(recipientUserId: userId, tenantId: tenantId), TestContext.Current.CancellationToken);
        }

        // Insert 1 read notification via direct DB manipulation
        UserNotification readNotification = BuildNotification(recipientUserId: userId, tenantId: tenantId);
        readNotification.State = UserNotificationState.Read;
        readNotification.ReadAt = DateTimeOffset.UtcNow;
        await _store.InsertAsync(readNotification, TestContext.Current.CancellationToken);

        int count = await _store.GetUnreadCountAsync(userId, tenantId, TestContext.Current.CancellationToken);

        count.Should().Be(3);
    }

    [Fact]
    public async Task MarkAsReadAsync_SetsStateAndReadAt()
    {
        UserNotification notification = BuildNotification();
        await _store.InsertAsync(notification, TestContext.Current.CancellationToken);

        DateTimeOffset readAt = DateTimeOffset.UtcNow;
        await _store.MarkAsReadAsync(notification.Id, readAt, TestContext.Current.CancellationToken);

        UserNotification? result = await _store.GetAsync(notification.Id, TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!.State.Should().Be(UserNotificationState.Read);
        result.ReadAt.Should().BeCloseTo(readAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task MarkAllAsReadAsync_MarksAllUnreadAsRead()
    {
        string userId = "user-mark-all";
        Guid tenantId = Guid.NewGuid();

        // Insert 3 unread notifications
        for (int i = 0; i < 3; i++)
        {
            await _store.InsertAsync(BuildNotification(recipientUserId: userId, tenantId: tenantId), TestContext.Current.CancellationToken);
        }

        DateTimeOffset readAt = DateTimeOffset.UtcNow;
        await _store.MarkAllAsReadAsync(userId, tenantId, readAt, TestContext.Current.CancellationToken);

        int unreadCount = await _store.GetUnreadCountAsync(userId, tenantId, TestContext.Current.CancellationToken);
        unreadCount.Should().Be(0);

        IReadOnlyList<UserNotification> all = await _store.GetListAsync(userId, tenantId, skipCount: 0, maxResultCount: 100, TestContext.Current.CancellationToken);
        all.Should().OnlyContain(n => n.State == UserNotificationState.Read);
    }

    [Fact]
    public async Task GetByEntityAsync_FiltersCorrectly()
    {
        Guid tenantId = Guid.NewGuid();
        string entityType = "Order";
        string entityId = "order-42";

        await _store.InsertAsync(BuildNotification(tenantId: tenantId, relatedEntityType: entityType, relatedEntityId: entityId), TestContext.Current.CancellationToken);
        await _store.InsertAsync(BuildNotification(tenantId: tenantId, relatedEntityType: entityType, relatedEntityId: entityId), TestContext.Current.CancellationToken);
        await _store.InsertAsync(BuildNotification(tenantId: tenantId, relatedEntityType: entityType, relatedEntityId: "order-99"), TestContext.Current.CancellationToken);
        await _store.InsertAsync(BuildNotification(tenantId: tenantId, relatedEntityType: "Invoice", relatedEntityId: entityId), TestContext.Current.CancellationToken);

        IReadOnlyList<UserNotification> result = await _store.GetByEntityAsync(entityType, entityId, tenantId, skipCount: 0, maxResultCount: 100, TestContext.Current.CancellationToken);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(n => n.RelatedEntityType == entityType && n.RelatedEntityId == entityId);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static UserNotification BuildNotification(
        string recipientUserId = "user-1",
        Guid? tenantId = null,
        DateTimeOffset? createdAt = null,
        string? relatedEntityType = null,
        string? relatedEntityId = null) => new()
        {
            Id = Guid.NewGuid(),
            NotificationId = Guid.NewGuid(),
            NotificationTypeName = "test.notification",
            Severity = NotificationSeverity.Info,
            RecipientUserId = recipientUserId,
            Data = JsonSerializer.SerializeToElement(new { key = "value" }),
            State = UserNotificationState.Unread,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
            TenantId = tenantId,
            RelatedEntityType = relatedEntityType,
            RelatedEntityId = relatedEntityId,
        };
}
