using Granit.Notifications.Sse.Options;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Sse.Tests;

public sealed class SseChannelOptionsTests
{
    [Fact]
    public void SectionName_IsCorrect() =>
        SseChannelOptions.SectionName.ShouldBe("Notifications:Sse");

    [Fact]
    public void HeartbeatIntervalSeconds_DefaultIs30()
    {
        SseChannelOptions options = new();

        options.HeartbeatIntervalSeconds.ShouldBe(30);
    }
}
