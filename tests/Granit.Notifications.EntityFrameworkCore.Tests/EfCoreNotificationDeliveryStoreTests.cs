// =============================================================================
// Tests - EfCoreNotificationDeliveryStore
// =============================================================================
// Verifies INSERT-only HDS audit trail: record single attempt,
// record multiple attempts for the same notification.
// =============================================================================

using FluentAssertions;
using Granit.Notifications.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Granit.Notifications.EntityFrameworkCore.Tests;

public sealed class EfCoreNotificationDeliveryStoreTests : IDisposable
{
    private readonly TestDbContextFactory _factory = TestDbContextFactory.Create();
    private readonly EfCoreNotificationDeliveryStore _store;

    public EfCoreNotificationDeliveryStoreTests()
    {
        _store = new EfCoreNotificationDeliveryStore(_factory);
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task RecordAsync_InsertsAttempt()
    {
        NotificationDeliveryAttempt attempt = BuildAttempt();

        await _store.RecordAsync(attempt, TestContext.Current.CancellationToken);

        await using NotificationDbContext db = _factory.CreateDbContext();
        NotificationDeliveryAttempt? result = await db.DeliveryAttempts.FindAsync([attempt.Id], TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!.NotificationId.Should().Be(attempt.NotificationId);
        result.ChannelName.Should().Be(attempt.ChannelName);
        result.IsSuccess.Should().Be(attempt.IsSuccess);
        result.RecipientUserId.Should().Be(attempt.RecipientUserId);
    }

    [Fact]
    public async Task RecordAsync_MultipleAttempts_AllPersisted()
    {
        Guid notificationId = Guid.NewGuid();
        NotificationDeliveryAttempt attempt1 = BuildAttempt(notificationId: notificationId, channelName: "email", isSuccess: false, errorMessage: "SMTP timeout");
        NotificationDeliveryAttempt attempt2 = BuildAttempt(notificationId: notificationId, channelName: "email", isSuccess: true);
        NotificationDeliveryAttempt attempt3 = BuildAttempt(notificationId: notificationId, channelName: "sms", isSuccess: true);

        await _store.RecordAsync(attempt1, TestContext.Current.CancellationToken);
        await _store.RecordAsync(attempt2, TestContext.Current.CancellationToken);
        await _store.RecordAsync(attempt3, TestContext.Current.CancellationToken);

        await using NotificationDbContext db = _factory.CreateDbContext();
        List<NotificationDeliveryAttempt> all = await db.DeliveryAttempts
            .Where(a => a.NotificationId == notificationId)
            .ToListAsync(TestContext.Current.CancellationToken);

        all.Should().HaveCount(3);
        all.Where(a => a.ChannelName == "email").Should().HaveCount(2);
        all.Where(a => a.IsSuccess).Should().HaveCount(2);
        all.Single(a => !a.IsSuccess).ErrorMessage.Should().Be("SMTP timeout");
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static NotificationDeliveryAttempt BuildAttempt(
        Guid? notificationId = null,
        string channelName = "email",
        bool isSuccess = true,
        string? errorMessage = null) => new()
        {
            Id = Guid.NewGuid(),
            DeliveryId = Guid.NewGuid(),
            NotificationId = notificationId ?? Guid.NewGuid(),
            NotificationTypeName = "test.notification",
            ChannelName = channelName,
            RecipientUserId = "user-1",
            TenantId = Guid.NewGuid(),
            OccurredAt = DateTimeOffset.UtcNow,
            DurationMs = 150,
            IsSuccess = isSuccess,
            ErrorMessage = errorMessage,
        };
}
