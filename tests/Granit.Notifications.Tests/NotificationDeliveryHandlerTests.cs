// =============================================================================
// Tests - NotificationDeliveryHandler
// =============================================================================
// Verifies channel dispatch: no-op when channel not registered, successful
// delivery audit, failure audit with rethrow, and context field correctness.
// =============================================================================

using System.Text.Json;
using Granit.Guids;
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
using Shouldly;
using Xunit;

namespace Granit.Notifications.Tests;

public sealed class NotificationDeliveryHandlerTests
{
    private readonly INotificationChannel _channel = Substitute.For<INotificationChannel>();
    private readonly INotificationDeliveryWriter _deliveryWriter = Substitute.For<INotificationDeliveryWriter>();
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

        await Should.NotThrowAsync(act);
        await _deliveryWriter.DidNotReceive().RecordAsync(
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

        await _deliveryWriter.Received(1).RecordAsync(
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

        await Should.ThrowAsync<NotificationDeliveryException>(act);
        await _deliveryWriter.Received(1).RecordAsync(
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

        capturedContext.ShouldNotBeNull();
        capturedContext!.NotificationTypeName.ShouldBe(command.NotificationTypeName);
        capturedContext.RecipientUserId.ShouldBe(command.RecipientUserId);
        capturedContext.Data.ValueKind.ShouldNotBe(JsonValueKind.Undefined);
        capturedContext.DeliveryId.ShouldBe(command.DeliveryId);
        capturedContext.Severity.ShouldBe(command.Severity);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private NotificationDeliveryHandler BuildHandler(IReadOnlyList<INotificationChannel> channels) =>
        new(channels, _deliveryWriter, new SimpleGuidGenerator(), _clock, _logger);

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
