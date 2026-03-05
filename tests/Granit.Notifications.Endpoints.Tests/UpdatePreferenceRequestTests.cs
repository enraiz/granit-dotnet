using Shouldly;
using Xunit;

namespace Granit.Notifications.Endpoints.Tests;

public sealed class UpdatePreferenceRequestTests
{
    [Fact]
    public void Properties_SetCorrectly()
    {
        UpdatePreferenceRequest request = new()
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
