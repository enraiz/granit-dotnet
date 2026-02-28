using Shouldly;
using Xunit;

namespace Granit.Workflow.Tests;

/// <summary>
/// Tests for <see cref="WorkflowTransitionContext"/> (AsyncLocal ambient context).
/// </summary>
public sealed class WorkflowTransitionContextTests
{
    [Fact]
    public void SetComment_ShouldBeAvailableViaCurrent()
    {
        // Arrange & Act
        using IDisposable scope = WorkflowTransitionContext.SetComment("Regulatory justification");

        // Assert
        WorkflowTransitionContext.Current.ShouldNotBeNull();
        WorkflowTransitionContext.Current!.Comment.ShouldBe("Regulatory justification");
    }

    [Fact]
    public void Dispose_ShouldResetCurrent()
    {
        // Arrange
        IDisposable scope = WorkflowTransitionContext.SetComment("Some comment");

        // Act
        scope.Dispose();

        // Assert
        WorkflowTransitionContext.Current.ShouldBeNull();
    }

    [Fact]
    public async Task SetComment_ShouldFlowAcrossAsyncBoundaries()
    {
        // Arrange
        using IDisposable scope = WorkflowTransitionContext.SetComment("Async comment");

        // Act
        string? comment = await Task.Run(() => WorkflowTransitionContext.Current?.Comment);

        // Assert
        comment.ShouldBe("Async comment");
    }
}
