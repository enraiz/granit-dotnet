using Granit.Notifications.MobilePush.AwsSns.Diagnostics;
using Shouldly;
using Xunit;

namespace Granit.Notifications.MobilePush.AwsSns.Tests;

public sealed class NotificationsMobilePushAwsSnsActivitySourceTests
{
    [Fact]
    public void Name_IsExpected() =>
        NotificationsMobilePushAwsSnsActivitySource.Name.ShouldBe("Granit.Notifications.MobilePush.AwsSns");

    [Fact]
    public void Source_HasCorrectName() =>
        NotificationsMobilePushAwsSnsActivitySource.Source.Name.ShouldBe("Granit.Notifications.MobilePush.AwsSns");

    [Fact]
    public void Operations_Send_HasExpectedValue() =>
        NotificationsMobilePushAwsSnsActivitySource.Operations.Send.ShouldBe("sns-push.send");

    [Fact]
    public void Tags_DeviceCount_HasExpectedValue() =>
        NotificationsMobilePushAwsSnsActivitySource.Tags.DeviceCount.ShouldBe("sns-push.device_count");
}
