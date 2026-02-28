// =============================================================================
// Tests - NotificationDeliveryHandler
// =============================================================================
// Verifies channel dispatch: no-op when channel not registered, successful
// delivery audit, failure audit with rethrow, and context field correctness.
// =============================================================================

using System.Text.Json;
using FluentAssertions;
using Granit.Notifications.Abstractions;
using Granit.Notifications.Domain;
using Granit.Notifications.Exceptions;
using Granit.Notifications.Handlers;
using Granit.Notifications.Messages;
using Granit.Timing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Granit.Notifications.Tests;

public sealed class NotificationDeliveryHandlerTests
{
    private readonly INotificationChannel _channel = Substitute.For<INotificationChannel>();
    private readonly INotificationDeliveryStore _deliveryStore = Substitute.For<INotificationDeliveryStore>();
    private readonly IClock _clock;
    private readonly ILogger<NotificationDeliveryHandler> _logger = NullLogger<NotificationDeliveryHandler>.Instance;

    public NotificationDeliveryHandlerTests()
    {
        _clock = Substitute.For<IClock>();
        _clock.Now.Returns(_ => DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task HandleAsync_ChannelNotRegistered_LogsWarningAndReturns()
    {
        NotificationDeliveryHandler handler = BuildHandler(channels: []);
        DeliverNotificationCommand command = BuildCommand(channelName: "Unknown");

        Func<Task> act = () => handler.HandleAsync(command, TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
        await _deliveryStore.DidNotReceive().RecordAsync(
            Arg.Any<NotificationDeliveryAttempt>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ChannelRegistered_CallsSendAsync()
    {
        _channel.Name.Returns(NotificationChannels.InApp);
        NotificationDeliveryHandler handler = BuildHandler(channels: [_channel]);
        DeliverNotificationCommand command = BuildCommand(channelName: NotificationChannels.InApp);

        await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        await _channel.Received(1).SendAsync(
            Arg.Any<NotificationDeliveryContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ChannelSucceeds_RecordsSuccessInDeliveryStore()
    {
        _channel.Name.Returns(NotificationChannels.InApp);
        NotificationDeliveryHandler handler = BuildHandler(channels: [_channel]);
        DeliverNotificationCommand command = BuildCommand(channelName: NotificationChannels.InApp);

        await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        await _deliveryStore.Received(1).RecordAsync(
            Arg.Is<NotificationDeliveryAttempt>(r => r.IsSuccess),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ChannelThrows_RecordsFailureAndRethrowsAsDeliveryException()
    {
        _channel.Name.Returns(NotificationChannels.InApp);
        _channel.SendAsync(Arg.Any<NotificationDeliveryContext>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Channel failed"));
        NotificationDeliveryHandler handler = BuildHandler(channels: [_channel]);
        DeliverNotificationCommand command = BuildCommand(channelName: NotificationChannels.InApp);

        Func<Task> act = () => handler.HandleAsync(command, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<NotificationDeliveryException>();
        await _deliveryStore.Received(1).RecordAsync(
            Arg.Is<NotificationDeliveryAttempt>(r => !r.IsSuccess),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ContextContainsCorrectFields()
    {
        _channel.Name.Returns(NotificationChannels.InApp);
        NotificationDeliveryContext? capturedContext = null;
        _channel.SendAsync(Arg.Any<NotificationDeliveryContext>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedContext = callInfo.Arg<NotificationDeliveryContext>();
                return Task.CompletedTask;
            });

        NotificationDeliveryHandler handler = BuildHandler(channels: [_channel]);
        DeliverNotificationCommand command = BuildCommand(channelName: NotificationChannels.InApp);

        await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        capturedContext.Should().NotBeNull();
        capturedContext!.NotificationTypeName.Should().Be(command.NotificationTypeName);
        capturedContext.RecipientUserId.Should().Be(command.RecipientUserId);
        capturedContext.Data.ValueKind.Should().NotBe(JsonValueKind.Undefined);
        capturedContext.DeliveryId.Should().Be(command.DeliveryId);
        capturedContext.Severity.Should().Be(command.Severity);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private NotificationDeliveryHandler BuildHandler(IReadOnlyList<INotificationChannel> channels) =>
        new(channels, _deliveryStore, _clock, _logger);

    private static DeliverNotificationCommand BuildCommand(string channelName = NotificationChannels.InApp) => new()
    {
        DeliveryId = Guid.NewGuid(),
        NotificationId = Guid.NewGuid(),
        NotificationTypeName = "test.notification",
        RecipientUserId = "user-1",
        ChannelName = channelName,
        Severity = NotificationSeverity.Info,
        Data = JsonSerializer.SerializeToElement(new { key = "value" }),
        OccurredAt = DateTimeOffset.UtcNow,
    };
}
