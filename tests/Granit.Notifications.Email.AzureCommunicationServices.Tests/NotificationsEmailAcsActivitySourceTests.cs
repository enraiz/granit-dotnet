using Granit.Notifications.Email.AzureCommunicationServices.Diagnostics;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Email.AzureCommunicationServices.Tests;

public sealed class NotificationsEmailAcsActivitySourceTests
{
    [Fact]
    public void Source_HasCorrectName() =>
        NotificationsEmailAcsActivitySource.Source.Name
            .ShouldBe("Granit.Notifications.Email.AzureCommunicationServices");

    [Fact]
    public void Operations_SendEmail_HasCorrectValue() =>
        NotificationsEmailAcsActivitySource.Operations.SendEmail
            .ShouldBe("acs-email.send");

    [Fact]
    public void Tags_To_HasCorrectValue() =>
        NotificationsEmailAcsActivitySource.Tags.To
            .ShouldBe("acs.email.to");

    [Fact]
    public void Tags_SubjectLength_HasCorrectValue() =>
        NotificationsEmailAcsActivitySource.Tags.SubjectLength
            .ShouldBe("acs.email.subject_length");
}
