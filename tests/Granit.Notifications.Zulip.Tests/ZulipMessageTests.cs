using Shouldly;
using Xunit;

namespace Granit.Notifications.Zulip.Tests;

public sealed class ZulipMessageTests
{
    [Fact]
    public void StreamMessage_SetsAllProperties()
    {
        var message = new ZulipMessage
        {
            Type = "stream",
            Stream = "alerts",
            Topic = "errors",
            Content = "Something went wrong",
        };

        message.Type.ShouldBe("stream");
        message.Stream.ShouldBe("alerts");
        message.Topic.ShouldBe("errors");
        message.Content.ShouldBe("Something went wrong");
        message.To.ShouldBeNull();
    }

    [Fact]
    public void DirectMessage_SetsRecipients()
    {
        var message = new ZulipMessage
        {
            Type = "direct",
            To = ["user1@example.com", "user2@example.com"],
            Content = "Hello there",
        };

        message.Type.ShouldBe("direct");
        message.To.ShouldNotBeNull();
        message.To!.Count.ShouldBe(2);
        message.Stream.ShouldBeNull();
        message.Topic.ShouldBeNull();
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = new ZulipMessage { Type = "stream", Stream = "s", Topic = "t", Content = "c" };
        var b = new ZulipMessage { Type = "stream", Stream = "s", Topic = "t", Content = "c" };

        a.ShouldBe(b);
    }

    [Fact]
    public void Equality_DifferentContent_AreNotEqual()
    {
        var a = new ZulipMessage { Type = "stream", Content = "a" };
        var b = new ZulipMessage { Type = "stream", Content = "b" };

        a.ShouldNotBe(b);
    }
}
