using Granit.Workflow.Domain;
using Granit.Workflow.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Granit.Workflow.EntityFrameworkCore.Tests;

/// <summary>
/// Tests for <see cref="WorkflowModelBuilderExtensions"/>.
/// </summary>
public sealed class WorkflowModelBuilderExtensionsTests
{
    [Fact]
    public void ConfigureWorkflowModule_ShouldRegisterWorkflowTransitionRecordEntity()
    {
        // Arrange & Act
        DbContextOptions<TestConfigDbContext> options = new DbContextOptionsBuilder<TestConfigDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using TestConfigDbContext context = new(options);

        // Assert — the entity type should be registered
        Microsoft.EntityFrameworkCore.Metadata.IEntityType? entityType =
            context.Model.FindEntityType(typeof(WorkflowTransitionRecord));
        entityType.ShouldNotBeNull();
    }

    [Fact]
    public void ConfigureWorkflowModule_ShouldMapToCorrectTableName()
    {
        // Arrange & Act
        DbContextOptions<TestConfigDbContext> options = new DbContextOptionsBuilder<TestConfigDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using TestConfigDbContext context = new(options);

        // Assert
        Microsoft.EntityFrameworkCore.Metadata.IEntityType entityType =
            context.Model.FindEntityType(typeof(WorkflowTransitionRecord))!;
        string? tableName = entityType.GetTableName();
        tableName.ShouldBe("workflow_transition_records");
    }

    // ========================================================================
    // Test DbContext
    // ========================================================================

    private sealed class TestConfigDbContext(DbContextOptions<TestConfigDbContext> options)
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
