// =============================================================================
// Tests - SignalRNotificationChannel
// =============================================================================
// Verifies the SignalR channel implementation: correct group targeting by
// recipient user ID, field mapping to SignalRNotificationMessage, and
// handling of optional RelatedEntity.
// =============================================================================

using System.Text.Json;
using FluentAssertions;
using Granit.Notifications.SignalR;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using Xunit;

namespace Granit.Notifications.SignalR.Tests;

public sealed class SignalRNotificationChannelTests
{
    private readonly IHubContext<NotificationHub> _hubContext = Substitute.For<IHubContext<NotificationHub>>();
    private readonly IHubClients _clients = Substitute.For<IHubClients>();
    private readonly IClientProxy _clientProxy = Substitute.For<IClientProxy>();
    private readonly SignalRNotificationChannel _channel;

    public SignalRNotificationChannelTests()
    {
        _hubContext.Clients.Returns(_clients);
        _clients.Group(Arg.Any<string>()).Returns(_clientProxy);
        _channel = new SignalRNotificationChannel(_hubContext);
    }

    [Fact]
    public void Name_ReturnsSignalR()
    {
        _channel.Name.Should().Be(NotificationChannels.SignalR);
    }

    [Fact]
    public async Task SendAsync_SendsToRecipientGroup()
    {
        NotificationDeliveryContext context = BuildContext();

        await _channel.SendAsync(context, TestContext.Current.CancellationToken);

        _clients.Received(1).Group(context.RecipientUserId);
    }

    [Fact]
    public async Task SendAsync_MapsFieldsCorrectly()
    {
        NotificationDeliveryContext context = BuildContext();
        SignalRNotificationMessage? captured = null;
        _clientProxy.SendCoreAsync(
            "ReceiveNotification",
            Arg.Do<object?[]>(args => { captured = args[0] as SignalRNotificationMessage; }),
            Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await _channel.SendAsync(context, TestContext.Current.CancellationToken);

        captured.Should().NotBeNull();
        captured!.NotificationId.Should().Be(context.NotificationId);
        captured.NotificationTypeName.Should().Be(context.NotificationTypeName);
        captured.Severity.Should().Be(context.Severity);
        captured.Data.GetProperty("key").GetString().Should().Be("value");
        captured.OccurredAt.Should().Be(context.OccurredAt);
    }

    [Fact]
    public async Task SendAsync_MapsRelatedEntity_WhenPresent()
    {
        NotificationDeliveryContext context = BuildContext(withRelatedEntity: true);
        SignalRNotificationMessage? captured = null;
        _clientProxy.SendCoreAsync(
            "ReceiveNotification",
            Arg.Do<object?[]>(args => { captured = args[0] as SignalRNotificationMessage; }),
            Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await _channel.SendAsync(context, TestContext.Current.CancellationToken);

        captured.Should().NotBeNull();
        captured!.RelatedEntityType.Should().Be("Patient");
        captured.RelatedEntityId.Should().Be("pat-1");
    }

    [Fact]
    public async Task SendAsync_NullRelatedEntity_SetsNullFields()
    {
        NotificationDeliveryContext context = BuildContext(withRelatedEntity: false);
        SignalRNotificationMessage? captured = null;
        _clientProxy.SendCoreAsync(
            "ReceiveNotification",
            Arg.Do<object?[]>(args => { captured = args[0] as SignalRNotificationMessage; }),
            Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await _channel.SendAsync(context, TestContext.Current.CancellationToken);

        captured.Should().NotBeNull();
        captured!.RelatedEntityType.Should().BeNull();
        captured.RelatedEntityId.Should().BeNull();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static NotificationDeliveryContext BuildContext(bool withRelatedEntity = true) => new()
    {
        DeliveryId = Guid.NewGuid(),
        NotificationId = Guid.NewGuid(),
        NotificationTypeName = "test.notification",
        RecipientUserId = "user-1",
        Severity = NotificationSeverity.Info,
        Data = JsonSerializer.SerializeToElement(new { key = "value" }),
        RelatedEntity = withRelatedEntity ? new EntityReference("Patient", "pat-1") : null,
        OccurredAt = DateTimeOffset.UtcNow,
    };
}
