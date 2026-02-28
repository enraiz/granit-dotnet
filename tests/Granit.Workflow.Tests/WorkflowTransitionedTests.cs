using Granit.Core.Events;
using Granit.Workflow.Domain;
using Granit.Workflow.Events;
using Shouldly;
using Xunit;

namespace Granit.Workflow.Tests;

/// <summary>
/// Tests for <see cref="WorkflowTransitioned{TState}"/> domain event record.
/// </summary>
public sealed class WorkflowTransitionedTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        // Arrange & Act
        WorkflowTransitioned<WorkflowLifecycleStatus> evt = new(
            EntityType: "Document",
            EntityId: "abc-123",
            PreviousState: WorkflowLifecycleStatus.Draft,
            NewState: WorkflowLifecycleStatus.Published,
            TransitionedBy: "user-42");

        // Assert
        evt.EntityType.ShouldBe("Document");
        evt.EntityId.ShouldBe("abc-123");
        evt.PreviousState.ShouldBe(WorkflowLifecycleStatus.Draft);
        evt.NewState.ShouldBe(WorkflowLifecycleStatus.Published);
        evt.TransitionedBy.ShouldBe("user-42");
    }

    [Fact]
    public void Record_ShouldImplementIDomainEvent()
    {
        // Arrange & Act
        WorkflowTransitioned<WorkflowLifecycleStatus> evt = new(
            "Document", "id-1", WorkflowLifecycleStatus.Draft,
            WorkflowLifecycleStatus.PendingReview, "user-1");

        // Assert
        evt.ShouldBeAssignableTo<IDomainEvent>();
    }

    [Fact]
    public void Record_ShouldSupportValueEquality()
    {
        // Arrange
        WorkflowTransitioned<WorkflowLifecycleStatus> evt1 = new(
            "Document", "id-1", WorkflowLifecycleStatus.Draft,
            WorkflowLifecycleStatus.Published, "user-1");

        WorkflowTransitioned<WorkflowLifecycleStatus> evt2 = new(
            "Document", "id-1", WorkflowLifecycleStatus.Draft,
            WorkflowLifecycleStatus.Published, "user-1");

        // Assert
        evt1.ShouldBe(evt2);
    }

    [Fact]
    public void Record_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        WorkflowTransitioned<WorkflowLifecycleStatus> evt1 = new(
            "Document", "id-1", WorkflowLifecycleStatus.Draft,
            WorkflowLifecycleStatus.Published, "user-1");

        WorkflowTransitioned<WorkflowLifecycleStatus> evt2 = new(
            "Document", "id-2", WorkflowLifecycleStatus.Draft,
            WorkflowLifecycleStatus.Published, "user-1");

        // Assert
        evt1.ShouldNotBe(evt2);
    }

    [Fact]
    public void Record_ShouldSupportWith()
    {
        // Arrange
        WorkflowTransitioned<WorkflowLifecycleStatus> original = new(
            "Document", "id-1", WorkflowLifecycleStatus.Draft,
            WorkflowLifecycleStatus.Published, "user-1");

        // Act
        WorkflowTransitioned<WorkflowLifecycleStatus> copy = original with { TransitionedBy = "user-2" };

        // Assert
        copy.TransitionedBy.ShouldBe("user-2");
        copy.EntityId.ShouldBe("id-1");
    }
}
