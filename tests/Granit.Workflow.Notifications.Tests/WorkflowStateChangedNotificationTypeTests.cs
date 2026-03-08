using Granit.Notifications;
using Shouldly;
using Xunit;

namespace Granit.Workflow.Notifications.Tests;

public sealed class WorkflowStateChangedNotificationTypeTests
{
    [Fact]
    public void Name_ReturnsExpected() =>
        WorkflowStateChangedNotificationType.Instance.Name.ShouldBe("workflow.state_changed");

    [Fact]
    public void DefaultSeverity_IsInfo() =>
        WorkflowStateChangedNotificationType.Instance.DefaultSeverity.ShouldBe(NotificationSeverity.Info);

    [Fact]
    public void DefaultChannels_ContainsInAppAndSignalR()
    {
        IReadOnlyList<string> channels = WorkflowStateChangedNotificationType.Instance.DefaultChannels;
        channels.ShouldContain(NotificationChannels.InApp);
        channels.ShouldContain(NotificationChannels.SignalR);
    }
}
