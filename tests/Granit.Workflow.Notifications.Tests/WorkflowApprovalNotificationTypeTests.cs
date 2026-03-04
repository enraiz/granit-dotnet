using Granit.Notifications;
using Shouldly;
using Xunit;

namespace Granit.Workflow.Notifications.Tests;

public sealed class WorkflowApprovalNotificationTypeTests
{
    [Fact]
    public void Instance_IsSingleton()
    {
        WorkflowApprovalNotificationType first = WorkflowApprovalNotificationType.Instance;
        WorkflowApprovalNotificationType second = WorkflowApprovalNotificationType.Instance;

        first.ShouldBeSameAs(second);
    }

    [Fact]
    public void Name_IsWorkflowApprovalRequested()
    {
        WorkflowApprovalNotificationType.Instance.Name
            .ShouldBe("workflow.approval_requested");
    }

    [Fact]
    public void DefaultSeverity_IsWarning()
    {
        WorkflowApprovalNotificationType.Instance.DefaultSeverity
            .ShouldBe(NotificationSeverity.Warning);
    }

    [Fact]
    public void DefaultChannels_ContainsInAppAndEmail()
    {
        IReadOnlyList<string> channels = WorkflowApprovalNotificationType.Instance.DefaultChannels;

        channels.Count.ShouldBe(2);
        channels.ShouldContain(NotificationChannels.InApp);
        channels.ShouldContain(NotificationChannels.Email);
    }

    [Fact]
    public void DataType_IsWorkflowApprovalNotificationData()
    {
        WorkflowApprovalNotificationType.Instance.DataType
            .ShouldBe(typeof(WorkflowApprovalNotificationData));
    }
}
