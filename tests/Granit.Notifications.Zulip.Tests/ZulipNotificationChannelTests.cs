using System.Text.Json;
using Granit.Notifications.Abstractions;
using Granit.Notifications.Zulip;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Zulip.Tests;

public sealed class ZulipNotificationChannelTests
{
    private readonly IZulipSender _sender = Substitute.For<IZulipSender>();
    private readonly IOptions<ZulipChannelOptions> _options;
    private readonly ZulipNotificationChannel _channel;

    public ZulipNotificationChannelTests()
    {
        _options = Options.Create(new ZulipChannelOptions { DefaultStream = "test-alerts", DefaultTopic = "system" });
        _channel = new ZulipNotificationChannel(_sender, _options, NullLogger<ZulipNotificationChannel>.Instance);
    }

    [Fact]
    public async Task SendAsync_SendsToDefaultStreamAndTopic()
    {
        NotificationDeliveryContext context = BuildContext();
        ZulipMessage? captured = null;
        _sender.SendAsync(Arg.Any<ZulipMessage>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => { captured = callInfo.Arg<ZulipMessage>(); return Task.CompletedTask; });

        await _channel.SendAsync(context, TestContext.Current.CancellationToken);

        captured.ShouldNotBeNull();
        captured!.Type.ShouldBe("stream");
        captured.Stream.ShouldBe("test-alerts");
        captured.Topic.ShouldBe("system");
    }

    [Fact]
    public async Task SendAsync_ContentContainsNotificationType()
    {
        NotificationDeliveryContext context = BuildContext();
        ZulipMessage? captured = null;
        _sender.SendAsync(Arg.Any<ZulipMessage>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => { captured = callInfo.Arg<ZulipMessage>(); return Task.CompletedTask; });

        await _channel.SendAsync(context, TestContext.Current.CancellationToken);

        captured.ShouldNotBeNull();
        captured!.Content.ShouldContain("test.notification");
    }

    [Fact]
    public void Name_ReturnsZulip() => _channel.Name.ShouldBe(NotificationChannels.Zulip);

    private static NotificationDeliveryContext BuildContext() => new()
    {
        DeliveryId = Guid.NewGuid(),
        NotificationId = Guid.NewGuid(),
        NotificationTypeName = "test.notification",
        RecipientUserId = "user-1",
        Severity = NotificationSeverity.Warning,
        Data = JsonSerializer.SerializeToElement(new { key = "value" }),
        OccurredAt = DateTimeOffset.UtcNow,
    };
}
