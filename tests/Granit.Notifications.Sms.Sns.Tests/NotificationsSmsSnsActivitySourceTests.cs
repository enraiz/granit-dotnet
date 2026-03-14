using Granit.Notifications.Sms.Sns.Diagnostics;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Sms.Sns.Tests;

public sealed class NotificationsSmsSnsActivitySourceTests
{
    [Fact]
    public void Name_IsExpected() =>
        NotificationsSmsSnsActivitySource.Name.ShouldBe("Granit.Notifications.Sms.Sns");

    [Fact]
    public void Source_HasCorrectName() =>
        NotificationsSmsSnsActivitySource.Source.Name.ShouldBe("Granit.Notifications.Sms.Sns");

    [Fact]
    public void Operations_SendSms_HasExpectedValue() =>
        NotificationsSmsSnsActivitySource.Operations.SendSms.ShouldBe("sns-sms.send");

    [Fact]
    public void Tags_Recipient_HasExpectedValue() =>
        NotificationsSmsSnsActivitySource.Tags.Recipient.ShouldBe("sns-sms.recipient");
}
