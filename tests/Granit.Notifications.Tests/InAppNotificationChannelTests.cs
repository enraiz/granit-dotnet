// =============================================================================
// Tests - InAppNotificationChannel
// =============================================================================
// Verifies the InApp channel implementation: notification insertion into the
// user notification store, correct field mapping, and initial Unread state.
// =============================================================================

using System.Text.Json;
using FluentAssertions;
using Granit.Notifications.Abstractions;
using Granit.Notifications.Domain;
using Granit.Notifications.Internal;
using Granit.Timing;
using NSubstitute;
using Xunit;

namespace Granit.Notifications.Tests;

public sealed class InAppNotificationChannelTests
{
    private readonly IUserNotificationStore _userNotificationStore = Substitute.For<IUserNotificationStore>();
    private readonly IClock _clock;
    private readonly InAppNotificationChannel _channel;

    public InAppNotificationChannelTests()
    {
        _clock = Substitute.For<IClock>();
        _clock.Now.Returns(_ => DateTimeOffset.UtcNow);
        _channel = new InAppNotificationChannel(_userNotificationStore, _clock);
    }

    [Fact]
    public async Task SendAsync_InsertsNotificationInStore()
    {
        NotificationDeliveryContext context = BuildContext();

        await _channel.SendAsync(context, TestContext.Current.CancellationToken);

        await _userNotificationStore.Received(1).InsertAsync(
            Arg.Any<UserNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_SetsCorrectFields()
    {
        NotificationDeliveryContext context = BuildContext();
        UserNotification? captured = null;
        _userNotificationStore.InsertAsync(Arg.Any<UserNotification>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                captured = callInfo.Arg<UserNotification>();
                return Task.CompletedTask;
            });

        await _channel.SendAsync(context, TestContext.Current.CancellationToken);

        captured.Should().NotBeNull();
        captured!.RecipientUserId.Should().Be(context.RecipientUserId);
        captured.NotificationTypeName.Should().Be(context.NotificationTypeName);
        captured.Severity.Should().Be(context.Severity);
    }

    [Fact]
    public async Task SendAsync_StateIsUnread()
    {
        NotificationDeliveryContext context = BuildContext();
        UserNotification? captured = null;
        _userNotificationStore.InsertAsync(Arg.Any<UserNotification>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                captured = callInfo.Arg<UserNotification>();
                return Task.CompletedTask;
            });

        await _channel.SendAsync(context, TestContext.Current.CancellationToken);

        captured.Should().NotBeNull();
        captured!.State.Should().Be(UserNotificationState.Unread);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static NotificationDeliveryContext BuildContext() => new()
    {
        DeliveryId = Guid.NewGuid(),
        NotificationId = Guid.NewGuid(),
        NotificationTypeName = "test.notification",
        RecipientUserId = "user-1",
        Severity = NotificationSeverity.Info,
        Data = JsonSerializer.SerializeToElement(new { key = "value" }),
        OccurredAt = DateTimeOffset.UtcNow,
    };
}
