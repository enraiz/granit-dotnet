using Granit.Notifications.MobilePush.Sns.Diagnostics;
using Shouldly;
using Xunit;

namespace Granit.Notifications.MobilePush.Sns.Tests;

public sealed class NotificationsMobilePushSnsActivitySourceTests
{
    [Fact]
    public void Name_IsExpected() =>
        NotificationsMobilePushSnsActivitySource.Name.ShouldBe("Granit.Notifications.MobilePush.Sns");

    [Fact]
    public void Source_HasCorrectName() =>
        NotificationsMobilePushSnsActivitySource.Source.Name.ShouldBe("Granit.Notifications.MobilePush.Sns");

    [Fact]
    public void Operations_Send_HasExpectedValue() =>
        NotificationsMobilePushSnsActivitySource.Operations.Send.ShouldBe("sns-push.send");

    [Fact]
    public void Tags_DeviceCount_HasExpectedValue() =>
        NotificationsMobilePushSnsActivitySource.Tags.DeviceCount.ShouldBe("sns-push.device_count");
}
