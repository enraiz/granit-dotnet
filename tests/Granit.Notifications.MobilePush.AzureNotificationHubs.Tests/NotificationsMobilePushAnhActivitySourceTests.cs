// =============================================================================
// Tests - NotificationsMobilePushAnhActivitySource
// =============================================================================
// Verifies the activity source constants and singleton instance.
// =============================================================================

using Granit.Notifications.MobilePush.AzureNotificationHubs.Diagnostics;
using Shouldly;
using Xunit;

namespace Granit.Notifications.MobilePush.AzureNotificationHubs.Tests;

public sealed class NotificationsMobilePushAnhActivitySourceTests
{
    [Fact]
    public void Name_IsFullyQualifiedPackageName() =>
        NotificationsMobilePushAnhActivitySource.Name
            .ShouldBe("Granit.Notifications.MobilePush.AzureNotificationHubs");

    [Fact]
    public void Source_IsNotNull() =>
        NotificationsMobilePushAnhActivitySource.Source.ShouldNotBeNull();

    [Fact]
    public void Source_Name_MatchesConstant() =>
        NotificationsMobilePushAnhActivitySource.Source.Name
            .ShouldBe(NotificationsMobilePushAnhActivitySource.Name);

    [Fact]
    public void Operations_Send_IsAnhDotSend() =>
        NotificationsMobilePushAnhActivitySource.Operations.Send.ShouldBe("anh.send");

    [Fact]
    public void Tags_DeviceCount_IsAnhDotDeviceCount() =>
        NotificationsMobilePushAnhActivitySource.Tags.DeviceCount.ShouldBe("anh.device_count");
}
