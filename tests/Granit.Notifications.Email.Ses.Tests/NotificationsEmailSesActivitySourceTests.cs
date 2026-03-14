using Granit.Notifications.Email.Ses.Diagnostics;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Email.Ses.Tests;

public sealed class NotificationsEmailSesActivitySourceTests
{
    [Fact]
    public void Source_HasCorrectName() =>
        NotificationsEmailSesActivitySource.Source.Name
            .ShouldBe("Granit.Notifications.Email.Ses");

    [Fact]
    public void Operations_SendEmail_HasCorrectValue() =>
        NotificationsEmailSesActivitySource.Operations.SendEmail
            .ShouldBe("ses.send-email");
}
