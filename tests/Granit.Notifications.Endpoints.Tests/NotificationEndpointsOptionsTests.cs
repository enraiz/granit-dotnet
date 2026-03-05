using Shouldly;
using Xunit;

namespace Granit.Notifications.Endpoints.Tests;

public sealed class NotificationEndpointsOptionsTests
{
    [Fact]
    public void ApiPrefix_Default_ShouldBeEmpty() =>
        new NotificationEndpointsOptions().ApiPrefix.ShouldBe(string.Empty);

    [Fact]
    public void RoutePrefix_Default_ShouldBeNotifications() =>
        new NotificationEndpointsOptions().RoutePrefix.ShouldBe("notifications");

    [Fact]
    public void TagName_Default_ShouldBeNotifications() =>
        new NotificationEndpointsOptions().TagName.ShouldBe("Notifications");
}
