using Granit.Workflow.Endpoints.Permissions;
using Shouldly;
using Xunit;

namespace Granit.Workflow.Endpoints.Tests;

/// <summary>
/// Verifies that <see cref="WorkflowPermissions"/> constants are correct and stable.
/// Permission names are persisted in databases and RBAC policies;
/// changing them is a breaking change.
/// </summary>
public sealed class WorkflowPermissionsTests
{
    [Fact]
    public void GroupName_is_Workflow() =>
        WorkflowPermissions.GroupName.ShouldBe("Workflow");

    [Fact]
    public void History_Read_is_Workflow_History_Read() =>
        WorkflowPermissions.History.Read.ShouldBe("Workflow.History.Read");

    [Fact]
    public void History_Read_starts_with_GroupName() =>
        WorkflowPermissions.History.Read.ShouldStartWith(WorkflowPermissions.GroupName + ".");
}
