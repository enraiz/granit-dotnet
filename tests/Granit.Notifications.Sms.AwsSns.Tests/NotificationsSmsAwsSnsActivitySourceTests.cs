using Granit.Notifications.Sms.AwsSns.Diagnostics;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Sms.AwsSns.Tests;

public sealed class NotificationsSmsAwsSnsActivitySourceTests
{
    [Fact]
    public void Name_IsExpected() =>
        NotificationsSmsAwsSnsActivitySource.Name.ShouldBe("Granit.Notifications.Sms.AwsSns");

    [Fact]
    public void Source_HasCorrectName() =>
        NotificationsSmsAwsSnsActivitySource.Source.Name.ShouldBe("Granit.Notifications.Sms.AwsSns");

    [Fact]
    public void Operations_SendSms_HasExpectedValue() =>
        NotificationsSmsAwsSnsActivitySource.Operations.SendSms.ShouldBe("sns-sms.send");

    [Fact]
    public void Tags_Recipient_HasExpectedValue() =>
        NotificationsSmsAwsSnsActivitySource.Tags.Recipient.ShouldBe("sns-sms.recipient");
}
