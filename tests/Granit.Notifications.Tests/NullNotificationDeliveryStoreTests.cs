// =============================================================================
// Tests - NullNotificationDeliveryStore
// =============================================================================
// Verifies the no-op delivery store used in development: RecordAsync completes
// without error and does not throw for any valid input.
// =============================================================================

using Granit.Notifications.Domain;
using Granit.Notifications.Internal;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Tests;

public sealed class NullNotificationDeliveryStoreTests
{
    private readonly NullNotificationDeliveryStore _store = new();

    [Fact]
    public async Task RecordAsync_CompletesSuccessfully()
    {
        NotificationDeliveryAttempt attempt = BuildAttempt(isSuccess: true);

        Func<Task> act = () => _store.RecordAsync(attempt, TestContext.Current.CancellationToken);

        await Should.NotThrowAsync(act);
    }

    [Fact]
    public async Task RecordAsync_WithFailedAttempt_CompletesSuccessfully()
    {
        NotificationDeliveryAttempt attempt = BuildAttempt(isSuccess: false, errorMessage: "Channel error");

        Func<Task> act = () => _store.RecordAsync(attempt, TestContext.Current.CancellationToken);

        await Should.NotThrowAsync(act);
    }

    [Fact]
    public async Task RecordAsync_ReturnsCompletedTask()
    {
        NotificationDeliveryAttempt attempt = BuildAttempt(isSuccess: true);

        Task result = _store.RecordAsync(attempt, TestContext.Current.CancellationToken);

        result.IsCompleted.ShouldBeTrue();
        await result;
    }

    [Fact]
    public async Task RecordAsync_MultipleCallsDoNotThrow()
    {
        for (int i = 0; i < 10; i++)
        {
            NotificationDeliveryAttempt attempt = BuildAttempt(isSuccess: i % 2 == 0);
            await _store.RecordAsync(attempt, TestContext.Current.CancellationToken);
        }

        // Null store is a no-op — verify it remains functional after multiple calls
        _store.ShouldNotBeNull();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static NotificationDeliveryAttempt BuildAttempt(
        bool isSuccess,
        string? errorMessage = null) => new()
        {
            DeliveryId = Guid.NewGuid(),
            NotificationId = Guid.NewGuid(),
            NotificationTypeName = "test.notification",
            ChannelName = NotificationChannels.InApp,
            RecipientUserId = "user-1",
            OccurredAt = DateTimeOffset.UtcNow,
            DurationMs = 42,
            IsSuccess = isSuccess,
            ErrorMessage = errorMessage,
        };
}
