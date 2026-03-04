using Shouldly;
using Xunit;

namespace Granit.Workflow.Notifications.Tests;

public sealed class WorkflowApprovalNotificationDataTests
{
    [Fact]
    public void Properties_MatchConstructorParameters()
    {
        WorkflowApprovalNotificationData data = new(
            EntityType: "Patient",
            EntityId: "123",
            RequestedBy: "user-1",
            TargetState: "Published",
            RequiredPermission: "workflow.publish");

        data.EntityType.ShouldBe("Patient");
        data.EntityId.ShouldBe("123");
        data.RequestedBy.ShouldBe("user-1");
        data.TargetState.ShouldBe("Published");
        data.RequiredPermission.ShouldBe("workflow.publish");
    }

    [Fact]
    public void Equality_TwoIdenticalRecords_AreEqual()
    {
        WorkflowApprovalNotificationData a = new("Patient", "1", "user", "Published", "perm");
        WorkflowApprovalNotificationData b = new("Patient", "1", "user", "Published", "perm");

        a.ShouldBe(b);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        WorkflowApprovalNotificationData a = new("Patient", "1", "user", "Published", "perm");
        WorkflowApprovalNotificationData b = new("Patient", "2", "user", "Published", "perm");

        a.ShouldNotBe(b);
    }
}
