using Granit.Workflow.Endpoints.Options;
using Shouldly;
using Xunit;

namespace Granit.Workflow.Endpoints.Tests;

/// <summary>
/// Verifies default values and mutability of <see cref="WorkflowEndpointsOptions"/>.
/// </summary>
public sealed class WorkflowEndpointsOptionsTests
{
    [Fact]
    public void SectionName_is_WorkflowEndpoints() =>
        WorkflowEndpointsOptions.SectionName.ShouldBe("WorkflowEndpoints");

    [Fact]
    public void Default_RoutePrefix_is_workflow()
    {
        WorkflowEndpointsOptions options = new();
        options.RoutePrefix.ShouldBe("workflow");
    }

    [Fact]
    public void Default_RequiredRole_is_granit_workflow_admin()
    {
        WorkflowEndpointsOptions options = new();
        options.RequiredRole.ShouldBe("granit-workflow-admin");
    }

    [Fact]
    public void Default_TagName_is_Workflow()
    {
        WorkflowEndpointsOptions options = new();
        options.TagName.ShouldBe("Workflow");
    }

    [Fact]
    public void Properties_are_mutable()
    {
        WorkflowEndpointsOptions options = new()
        {
            RoutePrefix = "wf",
            RequiredRole = "admin",
            TagName = "WF",
        };

        options.RoutePrefix.ShouldBe("wf");
        options.RequiredRole.ShouldBe("admin");
        options.TagName.ShouldBe("WF");
    }
}
