using Granit.Notifications.Email.SendGrid.Diagnostics;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Email.SendGrid.Tests;

public sealed class NotificationsEmailSendGridActivitySourceTests
{
    [Fact]
    public void Source_HasCorrectName() =>
        NotificationsEmailSendGridActivitySource.Source.Name
            .ShouldBe("Granit.Notifications.Email.SendGrid");

    [Fact]
    public void Operations_Send_HasCorrectValue() =>
        NotificationsEmailSendGridActivitySource.Operations.Send
            .ShouldBe("sendgrid.send");
}
