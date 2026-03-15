using Granit.Notifications.Email.AwsSes.Diagnostics;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Email.AwsSes.Tests;

public sealed class NotificationsEmailAwsSesActivitySourceTests
{
    [Fact]
    public void Source_HasCorrectName() =>
        NotificationsEmailAwsSesActivitySource.Source.Name
            .ShouldBe("Granit.Notifications.Email.AwsSes");

    [Fact]
    public void Operations_SendEmail_HasCorrectValue() =>
        NotificationsEmailAwsSesActivitySource.Operations.SendEmail
            .ShouldBe("ses.send-email");
}
