// =============================================================================
// Tests - PushNotificationChannel
// =============================================================================
// Verifies the Web Push channel implementation: no-subscription short circuit,
// name property, and store interaction when subscriptions are absent.
// PushServiceClient has no virtual methods so full send tests use a mock
// HttpMessageHandler to avoid real HTTP calls.
// =============================================================================

using System.Text.Json;
using FluentAssertions;
using Lib.Net.Http.WebPush;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Granit.Notifications.Push.Tests;

public sealed class PushNotificationChannelTests
{
    private readonly IPushSubscriptionStore _subscriptionStore = Substitute.For<IPushSubscriptionStore>();
    private readonly ILogger<PushNotificationChannel> _logger = Substitute.For<ILogger<PushNotificationChannel>>();

    [Fact]
    public void Name_ReturnsPush()
    {
        PushNotificationChannel channel = BuildChannel();

        channel.Name.Should().Be(NotificationChannels.Push);
    }

    [Fact]
    public async Task SendAsync_NoSubscriptions_DoesNotThrow()
    {
        _subscriptionStore.GetSubscriptionsAsync(
            Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<PushSubscriptionInfo>>([]));
        PushNotificationChannel channel = BuildChannel();
        NotificationDeliveryContext context = BuildContext();

        Func<Task> act = () => channel.SendAsync(context, TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendAsync_NoSubscriptions_QueriesStoreWithCorrectUser()
    {
        _subscriptionStore.GetSubscriptionsAsync(
            Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<PushSubscriptionInfo>>([]));
        PushNotificationChannel channel = BuildChannel();
        NotificationDeliveryContext context = BuildContext();

        await channel.SendAsync(context, TestContext.Current.CancellationToken);

        await _subscriptionStore.Received(1).GetSubscriptionsAsync(
            context.RecipientUserId, context.TenantId, Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private PushNotificationChannel BuildChannel()
    {
        MockHttpMessageHandler handler = new();
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("https://push.example.com") };
        PushServiceClient pushServiceClient = new(httpClient);
        return new PushNotificationChannel(pushServiceClient, _subscriptionStore, _logger);
    }

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
