using Granit.Notifications.Domain;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Tests.Domain;

public sealed class UserNotificationStateTests
{
    [Fact]
    public void Unread_HasValue_Zero() =>
        ((int)UserNotificationState.Unread).ShouldBe(0);

    [Fact]
    public void Read_HasValue_One() =>
        ((int)UserNotificationState.Read).ShouldBe(1);

    [Fact]
    public void HasExactlyTwoMembers() =>
        Enum.GetValues<UserNotificationState>().Length.ShouldBe(2);
}
