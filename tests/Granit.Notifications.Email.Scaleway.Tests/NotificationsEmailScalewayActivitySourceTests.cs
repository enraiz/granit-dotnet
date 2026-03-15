using Granit.Notifications.Email.Scaleway.Diagnostics;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Email.Scaleway.Tests;

public sealed class NotificationsEmailScalewayActivitySourceTests
{
    [Fact]
    public void Source_HasCorrectName() =>
        NotificationsEmailScalewayActivitySource.Source.Name
            .ShouldBe("Granit.Notifications.Email.Scaleway");

    [Fact]
    public void Operations_Send_HasCorrectValue() =>
        NotificationsEmailScalewayActivitySource.Operations.Send
            .ShouldBe("scaleway-email.send");
}
