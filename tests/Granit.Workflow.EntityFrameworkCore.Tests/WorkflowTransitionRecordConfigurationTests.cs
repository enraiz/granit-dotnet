using Granit.Workflow.Domain;
using Granit.Workflow.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Shouldly;
using Xunit;

namespace Granit.Workflow.EntityFrameworkCore.Tests;

/// <summary>
/// Tests for <c>WorkflowTransitionRecordConfiguration</c> (EF Core Fluent API).
/// Verifies the table name, column constraints, max lengths, and index definitions
/// applied to <see cref="WorkflowTransitionRecord"/>.
/// </summary>
public sealed class WorkflowTransitionRecordConfigurationTests
{
    private readonly IEntityType _entityType;

    public WorkflowTransitionRecordConfigurationTests()
    {
        DbContextOptions<ConfigTestDbContext> options = new DbContextOptionsBuilder<ConfigTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using ConfigTestDbContext context = new(options);
        _entityType = context.Model.FindEntityType(typeof(WorkflowTransitionRecord))!;
    }

    // ========================================================================
    // Table name
    // ========================================================================

    [Fact]
    public void Table_ShouldBeNamedWorkflowTransitionRecords() =>
        _entityType.GetTableName().ShouldBe("workflow_transition_records");

    // ========================================================================
    // Primary key
    // ========================================================================

    [Fact]
    public void PrimaryKey_ShouldBeId()
    {
        // Assert
        IKey? primaryKey = _entityType.FindPrimaryKey();
        primaryKey.ShouldNotBeNull();
        primaryKey.Properties.Count.ShouldBe(1);
        primaryKey.Properties[0].Name.ShouldBe("Id");
    }

    // ========================================================================
    // Required columns and max lengths
    // ========================================================================

    [Theory]
    [InlineData("EntityType", 200, false)]
    [InlineData("EntityId", 200, false)]
    [InlineData("PreviousState", 100, false)]
    [InlineData("NewState", 100, false)]
    [InlineData("TransitionedBy", 450, false)]
    public void Property_ShouldHaveCorrectMaxLengthAndBeRequired(
        string propertyName, int expectedMaxLength, bool isNullable)
    {
        // Arrange
        IProperty property = _entityType.FindProperty(propertyName)!;

        // Assert
        property.GetMaxLength().ShouldBe(expectedMaxLength);
        property.IsNullable.ShouldBe(isNullable);
    }

    [Fact]
    public void TransitionedAt_ShouldBeRequired()
    {
        // Arrange
        IProperty property = _entityType.FindProperty("TransitionedAt")!;

        // Assert
        property.IsNullable.ShouldBeFalse();
    }

    [Fact]
    public void Comment_ShouldHaveMaxLength2000AndBeNullable()
    {
        // Arrange
        IProperty property = _entityType.FindProperty("Comment")!;

        // Assert
        property.GetMaxLength().ShouldBe(2000);
        property.IsNullable.ShouldBeTrue();
    }

    [Fact]
    public void TenantId_ShouldBeNullable()
    {
        // Arrange
        IProperty property = _entityType.FindProperty("TenantId")!;

        // Assert
        property.IsNullable.ShouldBeTrue();
    }

    // ========================================================================
    // Indexes
    // ========================================================================

    [Fact]
    public void Index_EntityTypeEntityIdTransitionedAt_ShouldExist()
    {
        // Arrange & Act
        IIndex? index = FindIndexByDatabaseName("ix_workflow_transition_records_entity_type_id_at");

        // Assert
        index.ShouldNotBeNull();
        List<string> propertyNames = index.Properties.Select(p => p.Name).ToList();
        propertyNames.ShouldContain("EntityType");
        propertyNames.ShouldContain("EntityId");
        propertyNames.ShouldContain("TransitionedAt");
    }

    [Fact]
    public void Index_TenantIdTransitionedAt_ShouldExist()
    {
        // Arrange & Act
        IIndex? index = FindIndexByDatabaseName("ix_workflow_transition_records_tenantid_at");

        // Assert
        index.ShouldNotBeNull();
        List<string> propertyNames = index.Properties.Select(p => p.Name).ToList();
        propertyNames.ShouldContain("TenantId");
        propertyNames.ShouldContain("TransitionedAt");
    }

    [Fact]
    public void Index_TransitionedByTransitionedAt_ShouldExist()
    {
        // Arrange & Act
        IIndex? index = FindIndexByDatabaseName("ix_workflow_transition_records_by_user_at");

        // Assert
        index.ShouldNotBeNull();
        List<string> propertyNames = index.Properties.Select(p => p.Name).ToList();
        propertyNames.ShouldContain("TransitionedBy");
        propertyNames.ShouldContain("TransitionedAt");
    }

    // ========================================================================
    // Functional: can persist and retrieve records
    // ========================================================================

    [Fact]
    public async Task CanPersist_WorkflowTransitionRecord()
    {
        // Arrange
        DbContextOptions<ConfigTestDbContext> options = new DbContextOptionsBuilder<ConfigTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        Guid recordId = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        await using ConfigTestDbContext context = new(options);
        WorkflowTransitionRecord record = new()
        {
            Id = recordId,
            EntityType = "Document",
            EntityId = "doc-123",
            PreviousState = "Draft",
            NewState = "Published",
            TransitionedAt = now,
            TransitionedBy = "user-42",
            Comment = "Approved by manager",
            TenantId = Guid.NewGuid(),
        };

        context.WorkflowTransitionRecords.Add(record);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        WorkflowTransitionRecord? loaded = await context.WorkflowTransitionRecords
            .FindAsync([recordId], TestContext.Current.CancellationToken);

        // Assert
        loaded.ShouldNotBeNull();
        loaded.EntityType.ShouldBe("Document");
        loaded.EntityId.ShouldBe("doc-123");
        loaded.PreviousState.ShouldBe("Draft");
        loaded.NewState.ShouldBe("Published");
        loaded.TransitionedAt.ShouldBe(now);
        loaded.TransitionedBy.ShouldBe("user-42");
        loaded.Comment.ShouldBe("Approved by manager");
    }

    [Fact]
    public async Task CanPersist_WithNullComment()
    {
        // Arrange
        DbContextOptions<ConfigTestDbContext> options = new DbContextOptionsBuilder<ConfigTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        Guid recordId = Guid.NewGuid();

        await using ConfigTestDbContext context = new(options);
        WorkflowTransitionRecord record = new()
        {
            Id = recordId,
            EntityType = "Invoice",
            EntityId = "inv-1",
            PreviousState = "Draft",
            NewState = "PendingReview",
            TransitionedAt = DateTimeOffset.UtcNow,
            TransitionedBy = "user-1",
            Comment = null,
            TenantId = null,
        };

        context.WorkflowTransitionRecords.Add(record);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        WorkflowTransitionRecord? loaded = await context.WorkflowTransitionRecords
            .FindAsync([recordId], TestContext.Current.CancellationToken);

        // Assert
        loaded.ShouldNotBeNull();
        loaded.Comment.ShouldBeNull();
        loaded.TenantId.ShouldBeNull();
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    private IIndex? FindIndexByDatabaseName(string databaseName) =>
        _entityType.GetIndexes().FirstOrDefault(
            i => i.GetDatabaseName() == databaseName);

    // ========================================================================
    // Test DbContext
    // ========================================================================

    private sealed class ConfigTestDbContext(DbContextOptions<ConfigTestDbContext> options)
        : DbContext(options), IWorkflowDbContext
    {
        public DbSet<WorkflowTransitionRecord> WorkflowTransitionRecords => Set<WorkflowTransitionRecord>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ConfigureWorkflowModule();
        }
    }
}
