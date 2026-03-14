using Granit.Notifications.Sms.AzureCommunicationServices.Diagnostics;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Sms.AzureCommunicationServices.Tests;

public sealed class NotificationsSmsAcsActivitySourceTests
{
    [Fact]
    public void Source_HasCorrectName() =>
        NotificationsSmsAcsActivitySource.Source.Name
            .ShouldBe("Granit.Notifications.Sms.AzureCommunicationServices");

    [Fact]
    public void Operations_SendSms_HasCorrectValue() =>
        NotificationsSmsAcsActivitySource.Operations.SendSms
            .ShouldBe("acs-sms.send");
}
