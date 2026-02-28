// =============================================================================
// Tests - InMemoryUserNotificationStore
// =============================================================================
// Verifies the in-memory user notification store: insert/get, pagination,
// unread count, mark-as-read (single and bulk), entity filtering.
// =============================================================================

using System.Text.Json;
using FluentAssertions;
using Granit.Notifications.Domain;
using Granit.Notifications.Internal;
using Xunit;

namespace Granit.Notifications.Tests;

public sealed class InMemoryUserNotificationStoreTests
{
    private readonly InMemoryUserNotificationStore _store = new();

    [Fact]
    public async Task InsertAsync_ThenGetAsync_ReturnsNotification()
    {
        UserNotification notification = BuildNotification("user-1");

        await _store.InsertAsync(notification, TestContext.Current.CancellationToken);
        UserNotification? retrieved = await _store.GetAsync(notification.Id, TestContext.Current.CancellationToken);

        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(notification.Id);
    }

    [Fact]
    public async Task GetListAsync_ReturnsPaginatedResultsSortedByDate()
    {
        DateTimeOffset baseTime = new(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);

        UserNotification oldest = BuildNotification("user-1", createdAt: baseTime);
        UserNotification middle = BuildNotification("user-1", createdAt: baseTime.AddMinutes(1));
        UserNotification newest = BuildNotification("user-1", createdAt: baseTime.AddMinutes(2));

        await _store.InsertAsync(oldest, TestContext.Current.CancellationToken);
        await _store.InsertAsync(middle, TestContext.Current.CancellationToken);
        await _store.InsertAsync(newest, TestContext.Current.CancellationToken);

        IReadOnlyList<UserNotification> results =
            await _store.GetListAsync("user-1", tenantId: null, skipCount: 0, maxResultCount: 2, TestContext.Current.CancellationToken);

        results.Should().HaveCount(2);
        results[0].CreatedAt.Should().BeOnOrAfter(results[1].CreatedAt,
            "results should be sorted by date descending (newest first)");
    }

    [Fact]
    public async Task GetUnreadCountAsync_ReturnsCorrectCount()
    {
        UserNotification unread1 = BuildNotification("user-1", state: UserNotificationState.Unread);
        UserNotification unread2 = BuildNotification("user-1", state: UserNotificationState.Unread);
        UserNotification read = BuildNotification("user-1", state: UserNotificationState.Read);

        await _store.InsertAsync(unread1, TestContext.Current.CancellationToken);
        await _store.InsertAsync(unread2, TestContext.Current.CancellationToken);
        await _store.InsertAsync(read, TestContext.Current.CancellationToken);

        int count = await _store.GetUnreadCountAsync("user-1", tenantId: null, TestContext.Current.CancellationToken);

        count.Should().Be(2);
    }

    [Fact]
    public async Task MarkAsReadAsync_SetsStateAndReadAt()
    {
        UserNotification notification = BuildNotification("user-1");
        await _store.InsertAsync(notification, TestContext.Current.CancellationToken);

        DateTimeOffset readAt = DateTimeOffset.UtcNow;
        await _store.MarkAsReadAsync(notification.Id, readAt, TestContext.Current.CancellationToken);

        UserNotification? updated = await _store.GetAsync(notification.Id, TestContext.Current.CancellationToken);
        updated!.State.Should().Be(UserNotificationState.Read);
        updated.ReadAt.Should().Be(readAt);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_MarksAllUnreadAsRead()
    {
        UserNotification unread1 = BuildNotification("user-1", state: UserNotificationState.Unread);
        UserNotification unread2 = BuildNotification("user-1", state: UserNotificationState.Unread);
        UserNotification otherUser = BuildNotification("user-2", state: UserNotificationState.Unread);

        await _store.InsertAsync(unread1, TestContext.Current.CancellationToken);
        await _store.InsertAsync(unread2, TestContext.Current.CancellationToken);
        await _store.InsertAsync(otherUser, TestContext.Current.CancellationToken);

        DateTimeOffset readAt = DateTimeOffset.UtcNow;
        await _store.MarkAllAsReadAsync("user-1", tenantId: null, readAt, TestContext.Current.CancellationToken);

        UserNotification? updated1 = await _store.GetAsync(unread1.Id, TestContext.Current.CancellationToken);
        UserNotification? updated2 = await _store.GetAsync(unread2.Id, TestContext.Current.CancellationToken);
        UserNotification? otherUserNotif = await _store.GetAsync(otherUser.Id, TestContext.Current.CancellationToken);

        updated1!.State.Should().Be(UserNotificationState.Read);
        updated2!.State.Should().Be(UserNotificationState.Read);
        otherUserNotif!.State.Should().Be(UserNotificationState.Unread,
            "other user's notifications should not be affected");
    }

    [Fact]
    public async Task GetByEntityAsync_FiltersCorrectly()
    {
        UserNotification matching = BuildNotification("user-1", relatedEntityType: "Invoice", relatedEntityId: "inv-42");
        UserNotification nonMatching = BuildNotification("user-1", relatedEntityType: "Document", relatedEntityId: "doc-1");
        UserNotification noEntity = BuildNotification("user-1");

        await _store.InsertAsync(matching, TestContext.Current.CancellationToken);
        await _store.InsertAsync(nonMatching, TestContext.Current.CancellationToken);
        await _store.InsertAsync(noEntity, TestContext.Current.CancellationToken);

        IReadOnlyList<UserNotification> results =
            await _store.GetByEntityAsync("Invoice", "inv-42", tenantId: null, skipCount: 0, maxResultCount: 10, TestContext.Current.CancellationToken);

        results.Should().ContainSingle();
        results[0].Id.Should().Be(matching.Id);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static UserNotification BuildNotification(
        string userId,
        UserNotificationState state = UserNotificationState.Unread,
        DateTimeOffset? createdAt = null,
        string? relatedEntityType = null,
        string? relatedEntityId = null) => new()
        {
            Id = Guid.NewGuid(),
            RecipientUserId = userId,
            NotificationTypeName = "test.notification",
            Severity = NotificationSeverity.Info,
            Data = JsonSerializer.SerializeToElement(new { key = "value" }),
            State = state,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
            RelatedEntityType = relatedEntityType,
            RelatedEntityId = relatedEntityId,
        };
}
