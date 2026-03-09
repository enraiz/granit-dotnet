using Shouldly;
using Xunit;

namespace Granit.Security.Tests;

public sealed class ActorKindTests
{
    [Fact]
    public void ActorKind_HasExpectedValues()
    {
        Enum.GetNames<ActorKind>().ShouldBe(["User", "ExternalSystem", "System"]);
    }

    [Fact]
    public void User_IsDefault() => default(ActorKind).ShouldBe(ActorKind.User);
}
