using Granit.Workflow.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Granit.Workflow.Tests;

/// <summary>
/// Tests for the null-object <see cref="IWorkflowPermissionChecker"/> implementation
/// registered by <see cref="WorkflowServiceCollectionExtensions.AddGranitWorkflow"/>.
/// </summary>
public sealed class NullWorkflowPermissionCheckerTests
{
    [Fact]
    public async Task IsGrantedAsync_ShouldAlwaysReturnTrue()
    {
        // Arrange
        IWorkflowPermissionChecker checker = ResolveNullChecker();

        // Act
        bool result = await checker.IsGrantedAsync("any.permission", TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeTrue();
    }

    [Theory]
    [InlineData("workflow.submit")]
    [InlineData("workflow.publish")]
    [InlineData("workflow.archive")]
    [InlineData("non.existent.permission")]
    public async Task IsGrantedAsync_AnyPermission_ShouldReturnTrue(string permissionName)
    {
        // Arrange
        IWorkflowPermissionChecker checker = ResolveNullChecker();

        // Act
        bool result = await checker.IsGrantedAsync(permissionName, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeTrue();
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    private static IWorkflowPermissionChecker ResolveNullChecker()
    {
        ServiceCollection services = new();
        services.AddGranitWorkflow();
        ServiceProvider sp = services.BuildServiceProvider();
        using IServiceScope scope = sp.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IWorkflowPermissionChecker>();
    }
}
