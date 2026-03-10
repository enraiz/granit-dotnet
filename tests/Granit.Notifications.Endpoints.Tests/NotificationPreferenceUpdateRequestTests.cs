using Shouldly;
using Xunit;

namespace Granit.Notifications.Endpoints.Tests;

public sealed class NotificationPreferenceUpdateRequestTests
{
    [Fact]
    public void Properties_SetCorrectly()
    {
        NotificationPreferenceUpdateRequest request = new()
        {
            NotificationTypeName = "Order.Shipped",
            ChannelName = "Email",
            IsEnabled = true,
        };

        request.NotificationTypeName.ShouldBe("Order.Shipped");
        request.ChannelName.ShouldBe("Email");
        request.IsEnabled.ShouldBeTrue();
    }
}
