using Granit.Workflow.Definitions;
using Granit.Workflow.Domain;
using Shouldly;
using Xunit;

namespace Granit.Workflow.Tests;

/// <summary>
/// Tests for the pre-built <see cref="PublicationWorkflow"/> definition.
/// </summary>
public sealed class PublicationWorkflowTests
{
    [Fact]
    public void Default_ShouldHaveCorrectInitialState()
    {
        // Assert
        PublicationWorkflow.Default.InitialState.ShouldBe(WorkflowLifecycleStatus.Draft);
    }

    [Fact]
    public void Default_ShouldHaveExpectedTransitions()
    {
        // Assert
        PublicationWorkflow.Default.Transitions.Count.ShouldBe(5);
    }

    [Theory]
    [InlineData(WorkflowLifecycleStatus.Draft, 2)]           // PendingReview, Published
    [InlineData(WorkflowLifecycleStatus.PendingReview, 1)]    // Published
    [InlineData(WorkflowLifecycleStatus.Published, 2)]        // Archived, Draft
    [InlineData(WorkflowLifecycleStatus.Archived, 0)]         // Terminal state
    public void Default_ShouldHaveCorrectTransitionCounts(
        WorkflowLifecycleStatus state, int expectedCount)
    {
        // Act
        IReadOnlyList<WorkflowTransition<WorkflowLifecycleStatus>> transitions =
            PublicationWorkflow.Default.GetAllowedTransitions(state);

        // Assert
        transitions.Count.ShouldBe(expectedCount);
    }

    [Fact]
    public void Default_PublishTransition_ShouldRequireApproval()
    {
        // Arrange
        IReadOnlyList<WorkflowTransition<WorkflowLifecycleStatus>> transitions =
            PublicationWorkflow.Default.GetAllowedTransitions(WorkflowLifecycleStatus.PendingReview);

        // Assert
        WorkflowTransition<WorkflowLifecycleStatus> publishTransition = transitions[0];
        publishTransition.To.ShouldBe(WorkflowLifecycleStatus.Published);
        publishTransition.RequiresApproval.ShouldBeTrue();
        publishTransition.RequiredPermission.ShouldBe("workflow.publish");
    }
}
