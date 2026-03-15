using Granit.Notifications.Twilio.Options;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Twilio.Tests;

public sealed class TwilioOptionsTests
{
    [Fact]
    public void SectionName_IsNotificationsTwilio() =>
        TwilioOptions.SectionName.ShouldBe("Notifications:Twilio");

    [Fact]
    public void AccountSid_Default_IsEmpty() =>
        new TwilioOptions().AccountSid.ShouldBe(string.Empty);

    [Fact]
    public void AuthToken_Default_IsEmpty() =>
        new TwilioOptions().AuthToken.ShouldBe(string.Empty);

    [Fact]
    public void DefaultSmsFromNumber_Default_IsEmpty() =>
        new TwilioOptions().DefaultSmsFromNumber.ShouldBe(string.Empty);

    [Fact]
    public void DefaultWhatsAppFromNumber_Default_IsNull() =>
        new TwilioOptions().DefaultWhatsAppFromNumber.ShouldBeNull();

    [Fact]
    public void BaseUrl_Default_IsTwilioApi() =>
        new TwilioOptions().BaseUrl.ShouldBe("https://api.twilio.com");

    [Fact]
    public void TimeoutSeconds_Default_Is30() =>
        new TwilioOptions().TimeoutSeconds.ShouldBe(30);
}
