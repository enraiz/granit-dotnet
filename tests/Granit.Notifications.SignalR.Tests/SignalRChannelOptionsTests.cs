using Granit.Notifications.SignalR.Options;
using Shouldly;
using Xunit;

namespace Granit.Notifications.SignalR.Tests;

public sealed class SignalRChannelOptionsTests
{
    [Fact]
    public void SectionName_IsNotificationsSignalR() =>
        SignalRChannelOptions.SectionName.ShouldBe("Notifications:SignalR");

    [Fact]
    public void RedisConnectionString_Default_IsNull() =>
        new SignalRChannelOptions().RedisConnectionString.ShouldBeNull();

    [Fact]
    public void RedisConnectionString_CanBeSet()
    {
        SignalRChannelOptions options = new() { RedisConnectionString = "localhost:6379" };
        options.RedisConnectionString.ShouldBe("localhost:6379");
    }
}
