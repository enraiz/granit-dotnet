using System.Reflection;
using Granit.Workflow.Domain;
using Shouldly;
using Xunit;

namespace Granit.Workflow.Tests;

/// <summary>
/// Tests for <see cref="VersionedWorkflowEntity"/> base class.
/// </summary>
public sealed class VersionedWorkflowEntityTests
{
    // ========================================================================
    // Property defaults
    // ========================================================================

    [Fact]
    public void NewEntity_ShouldHaveDefaultPropertyValues()
    {
        // Arrange & Act
        TestVersionedWorkflowEntity entity = new();

        // Assert
        entity.BusinessId.ShouldBe(Guid.Empty);
        entity.Version.ShouldBe(0);
        entity.LifecycleStatus.ShouldBe(WorkflowLifecycleStatus.Draft);
        entity.IsPublished.ShouldBeFalse();
    }

    // ========================================================================
    // Property setters
    // ========================================================================

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        Guid businessId = Guid.NewGuid();
        TestVersionedWorkflowEntity entity = new()
        {
            BusinessId = businessId,
            Version = 3,
            LifecycleStatus = WorkflowLifecycleStatus.Published,
            IsPublished = true,
        };

        // Assert
        entity.BusinessId.ShouldBe(businessId);
        entity.Version.ShouldBe(3);
        entity.LifecycleStatus.ShouldBe(WorkflowLifecycleStatus.Published);
        entity.IsPublished.ShouldBeTrue();
    }

    // ========================================================================
    // GetWorkflowEntityId
    // ========================================================================

    [Fact]
    public void GetWorkflowEntityId_ShouldReturnIdAsString()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        TestVersionedWorkflowEntity entity = new() { Id = id };

        // Act
        string entityId = entity.GetWorkflowEntityId();

        // Assert
        entityId.ShouldBe(id.ToString());
    }

    // ========================================================================
    // IWorkflowStateful.StatusPropertyName (resolved via reflection)
    // ========================================================================

    [Fact]
    public void StatusPropertyName_ShouldReturnLifecycleStatus()
    {
        // Arrange & Act — access via static property (same path as interceptor)
        string propertyName = GetStaticProperty<string>(
            typeof(TestVersionedWorkflowEntity), "StatusPropertyName");

        // Assert
        propertyName.ShouldBe("LifecycleStatus");
    }

    // ========================================================================
    // IWorkflowStateful.WorkflowEntityType — derived class override
    // ========================================================================

    [Fact]
    public void WorkflowEntityType_DerivedOverride_ShouldReturnCustomValue()
    {
        // Arrange & Act — resolved via reflection as the interceptor does
        string entityType = GetStaticProperty<string>(
            typeof(TestVersionedWorkflowEntity), "WorkflowEntityType");

        // Assert
        entityType.ShouldBe("TestDocument");
    }

    // ========================================================================
    // AuditedEntity inheritance
    // ========================================================================

    [Fact]
    public void Entity_ShouldInheritAuditedEntityProperties()
    {
        // Arrange
        DateTimeOffset now = DateTimeOffset.UtcNow;
        TestVersionedWorkflowEntity entity = new()
        {
            Id = Guid.NewGuid(),
            CreatedAt = now,
            CreatedBy = "user-1",
            ModifiedAt = now,
            ModifiedBy = "user-2",
        };

        // Assert
        entity.CreatedAt.ShouldBe(now);
        entity.CreatedBy.ShouldBe("user-1");
        entity.ModifiedAt.ShouldBe(now);
        entity.ModifiedBy.ShouldBe("user-2");
    }

    // ========================================================================
    // IVersionedEntity implementation
    // ========================================================================

    [Fact]
    public void Entity_ShouldImplementIVersionedEntity()
    {
        // Arrange & Act
        TestVersionedWorkflowEntity entity = new()
        {
            BusinessId = Guid.NewGuid(),
            Version = 5,
            LifecycleStatus = WorkflowLifecycleStatus.Archived,
            IsPublished = false,
        };

        // Assert — cast to interface
        IVersionedEntity versionedEntity = entity;
        versionedEntity.BusinessId.ShouldBe(entity.BusinessId);
        versionedEntity.Version.ShouldBe(5);
        versionedEntity.LifecycleStatus.ShouldBe(WorkflowLifecycleStatus.Archived);
        versionedEntity.IsPublished.ShouldBeFalse();
    }

    [Fact]
    public void Entity_ShouldImplementIWorkflowStateful()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        TestVersionedWorkflowEntity entity = new() { Id = id };

        // Act — cast to interface
        IWorkflowStateful stateful = entity;

        // Assert
        stateful.GetWorkflowEntityId().ShouldBe(id.ToString());
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    private static T GetStaticProperty<T>(Type type, string propertyName) =>
        (T)type.GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)!
            .GetValue(null)!;

    // ========================================================================
    // Test entity — re-implements IWorkflowStateful to provide WorkflowEntityType
    // ========================================================================

    private sealed class TestVersionedWorkflowEntity : VersionedWorkflowEntity, IWorkflowStateful
    {
        public static string StatusPropertyName => nameof(LifecycleStatus);
        public static string WorkflowEntityType => "TestDocument";
    }
}
