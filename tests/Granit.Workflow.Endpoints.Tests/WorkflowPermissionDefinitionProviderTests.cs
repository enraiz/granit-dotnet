using Granit.Authorization.Abstractions;
using Granit.Workflow.Endpoints.Permissions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.Workflow.Endpoints.Tests;

/// <summary>
/// Verifies that <see cref="WorkflowPermissionDefinitionProvider"/> correctly
/// registers the Workflow permission group and the History permission.
/// </summary>
public sealed class WorkflowPermissionDefinitionProviderTests
{
    [Fact]
    public void DefinePermissions_creates_Workflow_group()
    {
        // Arrange
        IPermissionDefinitionContext context = Substitute.For<IPermissionDefinitionContext>();
        PermissionGroup group = new(WorkflowPermissions.GroupName, "Workflow");
        context.AddGroup(WorkflowPermissions.GroupName, "Workflow").Returns(group);

        WorkflowPermissionDefinitionProvider provider = new();

        // Act
        provider.DefinePermissions(context);

        // Assert
        context.Received(1).AddGroup(WorkflowPermissions.GroupName, "Workflow");
    }

    [Fact]
    public void DefinePermissions_adds_History_Default_permission()
    {
        // Arrange
        PermissionGroup group = new(WorkflowPermissions.GroupName, "Workflow");
        IPermissionDefinitionContext context = Substitute.For<IPermissionDefinitionContext>();
        context.AddGroup(WorkflowPermissions.GroupName, "Workflow").Returns(group);

        WorkflowPermissionDefinitionProvider provider = new();

        // Act
        provider.DefinePermissions(context);

        // Assert
        group.Permissions.ShouldContain(p => p.Name == WorkflowPermissions.History.Default);
    }

    [Fact]
    public void DefinePermissions_History_permission_has_display_name()
    {
        // Arrange
        PermissionGroup group = new(WorkflowPermissions.GroupName, "Workflow");
        IPermissionDefinitionContext context = Substitute.For<IPermissionDefinitionContext>();
        context.AddGroup(WorkflowPermissions.GroupName, "Workflow").Returns(group);

        WorkflowPermissionDefinitionProvider provider = new();

        // Act
        provider.DefinePermissions(context);

        // Assert
        PermissionDefinition historyPermission = group.Permissions
            .Single(p => p.Name == WorkflowPermissions.History.Default);
        historyPermission.DisplayName.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void DefinePermissions_registers_exactly_one_permission()
    {
        // Arrange
        PermissionGroup group = new(WorkflowPermissions.GroupName, "Workflow");
        IPermissionDefinitionContext context = Substitute.For<IPermissionDefinitionContext>();
        context.AddGroup(WorkflowPermissions.GroupName, "Workflow").Returns(group);

        WorkflowPermissionDefinitionProvider provider = new();

        // Act
        provider.DefinePermissions(context);

        // Assert
        group.Permissions.Count.ShouldBe(1);
    }
}
