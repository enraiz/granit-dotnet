using Granit.Notifications.MobilePush.GoogleFcm.Options;
using Shouldly;
using Xunit;

namespace Granit.Notifications.MobilePush.GoogleFcm.Tests;

public sealed class GoogleFcmOptionsTests
{
    [Fact]
    public void SectionName_IsCorrect() =>
        GoogleFcmOptions.SectionName.ShouldBe("Notifications:MobilePush:GoogleFcm");

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        GoogleFcmOptions options = new();

        options.ProjectId.ShouldBe(string.Empty);
        options.ServiceAccountJson.ShouldBe(string.Empty);
        options.BaseAddress.ShouldBe("https://fcm.googleapis.com/");
        options.TimeoutSeconds.ShouldBe(30);
    }
}
