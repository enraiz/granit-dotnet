using Granit.Workflow.Domain;
using Shouldly;
using Xunit;

namespace Granit.Workflow.Tests;

public sealed class WorkflowDefinitionBuilderTests
{
    // ========================================================================
    // Valid definitions
    // ========================================================================

    [Fact]
    public void Build_WithValidDefinition_ShouldCreateImmutableDefinition()
    {
        // Arrange & Act
        var definition = WorkflowDefinition<WorkflowLifecycleStatus>.Create(b => b
            .InitialState(WorkflowLifecycleStatus.Draft)
            .Transition(WorkflowLifecycleStatus.Draft, WorkflowLifecycleStatus.PendingReview)
            .Transition(WorkflowLifecycleStatus.PendingReview, WorkflowLifecycleStatus.Published)
            .Transition(WorkflowLifecycleStatus.Published, WorkflowLifecycleStatus.Archived));

        // Assert
        definition.InitialState.ShouldBe(WorkflowLifecycleStatus.Draft);
        definition.Transitions.Count.ShouldBe(3);
    }

    [Fact]
    public void Build_WithNamedTransitions_ShouldPreserveNames()
    {
        // Arrange & Act
        var definition = WorkflowDefinition<WorkflowLifecycleStatus>.Create(b => b
            .InitialState(WorkflowLifecycleStatus.Draft)
            .Transition(WorkflowLifecycleStatus.Draft, WorkflowLifecycleStatus.Published, t => t
                .Named("Publier")
                .RequiresPermission("document.publish")
                .RequiresApproval()));

        // Assert
        WorkflowTransition<WorkflowLifecycleStatus> transition = definition.Transitions[0];
        transition.Name.ShouldBe("Publier");
        transition.RequiredPermission.ShouldBe("document.publish");
        transition.RequiresApproval.ShouldBeTrue();
    }

    [Fact]
    public void GetAllowedTransitions_ShouldReturnOnlyFromMatchingState()
    {
        // Arrange
        var definition = WorkflowDefinition<WorkflowLifecycleStatus>.Create(b => b
            .InitialState(WorkflowLifecycleStatus.Draft)
            .Transition(WorkflowLifecycleStatus.Draft, WorkflowLifecycleStatus.PendingReview)
            .Transition(WorkflowLifecycleStatus.Draft, WorkflowLifecycleStatus.Published)
            .Transition(WorkflowLifecycleStatus.PendingReview, WorkflowLifecycleStatus.Published)
            .Transition(WorkflowLifecycleStatus.Published, WorkflowLifecycleStatus.Archived));

        // Act
        IReadOnlyList<WorkflowTransition<WorkflowLifecycleStatus>> fromDraft = definition.GetAllowedTransitions(WorkflowLifecycleStatus.Draft);
        IReadOnlyList<WorkflowTransition<WorkflowLifecycleStatus>> fromPublished = definition.GetAllowedTransitions(WorkflowLifecycleStatus.Published);

        // Assert
        fromDraft.Count.ShouldBe(2);
        fromPublished.Count.ShouldBe(1);
        fromPublished[0].To.ShouldBe(WorkflowLifecycleStatus.Archived);
    }

    [Fact]
    public void GetAllowedTransitions_FromStateWithNoTransitions_ShouldReturnEmpty()
    {
        // Arrange
        var definition = WorkflowDefinition<WorkflowLifecycleStatus>.Create(b => b
            .InitialState(WorkflowLifecycleStatus.Draft)
            .Transition(WorkflowLifecycleStatus.Draft, WorkflowLifecycleStatus.Published));

        // Act
        IReadOnlyList<WorkflowTransition<WorkflowLifecycleStatus>> fromArchived = definition.GetAllowedTransitions(WorkflowLifecycleStatus.Archived);

        // Assert
        fromArchived.ShouldBeEmpty();
    }

    // ========================================================================
    // Invalid definitions — graph validation at Build()
    // ========================================================================

    [Fact]
    public void Build_WithoutInitialState_ShouldThrow()
    {
        // Arrange & Act
        Action act = () => WorkflowDefinition<WorkflowLifecycleStatus>.Create(b => b
            .Transition(WorkflowLifecycleStatus.Draft, WorkflowLifecycleStatus.Published));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("initial state");
    }

    [Fact]
    public void Build_WithoutTransitions_ShouldThrow()
    {
        // Arrange & Act
        Action act = () => WorkflowDefinition<WorkflowLifecycleStatus>.Create(b => b
            .InitialState(WorkflowLifecycleStatus.Draft));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("at least one transition");
    }

    [Fact]
    public void Build_WithDuplicateTransition_ShouldThrow()
    {
        // Arrange & Act
        Action act = () => WorkflowDefinition<WorkflowLifecycleStatus>.Create(b => b
            .InitialState(WorkflowLifecycleStatus.Draft)
            .Transition(WorkflowLifecycleStatus.Draft, WorkflowLifecycleStatus.Published)
            .Transition(WorkflowLifecycleStatus.Draft, WorkflowLifecycleStatus.Published));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("duplicate");
    }

    [Fact]
    public void Build_WithUnreachableState_ShouldThrow()
    {
        // Arrange & Act — PendingReview is in a transition target but not reachable from Draft
        // Actually: Draft → Published and PendingReview → Archived means PendingReview is unreachable
        Action act = () => WorkflowDefinition<WorkflowLifecycleStatus>.Create(b => b
            .InitialState(WorkflowLifecycleStatus.Draft)
            .Transition(WorkflowLifecycleStatus.Draft, WorkflowLifecycleStatus.Published)
            .Transition(WorkflowLifecycleStatus.PendingReview, WorkflowLifecycleStatus.Archived));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("unreachable");
    }
}
