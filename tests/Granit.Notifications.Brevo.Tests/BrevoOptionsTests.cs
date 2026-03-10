using Shouldly;
using Xunit;

namespace Granit.Notifications.Brevo.Tests;

public sealed class BrevoOptionsTests
{
    [Fact]
    public void SectionName_IsNotificationsBrevo() =>
        BrevoOptions.SectionName.ShouldBe("Notifications:Brevo");

    [Fact]
    public void ApiKey_Default_IsEmpty() =>
        new BrevoOptions().ApiKey.ShouldBe(string.Empty);

    [Fact]
    public void DefaultSenderEmail_Default_IsEmpty() =>
        new BrevoOptions().DefaultSenderEmail.ShouldBe(string.Empty);

    [Fact]
    public void DefaultSenderName_Default_IsEmpty() =>
        new BrevoOptions().DefaultSenderName.ShouldBe(string.Empty);

    [Fact]
    public void DefaultSmsSenderId_Default_IsEmpty() =>
        new BrevoOptions().DefaultSmsSenderId.ShouldBe(string.Empty);

    [Fact]
    public void BaseUrl_Default_IsBrevoV3() =>
        new BrevoOptions().BaseUrl.ShouldBe("https://api.brevo.com/v3");

    [Fact]
    public void TimeoutSeconds_Default_Is30() =>
        new BrevoOptions().TimeoutSeconds.ShouldBe(30);
}
