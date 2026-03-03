using Granit.DataExchange.EntityFrameworkCore.Internal.Import.Execution;
using Granit.DataExchange.EntityFrameworkCore.Tests.Infrastructure;
using Granit.DataExchange.Import.Domain;
using Granit.DataExchange.Import.Execution;
using Granit.DataExchange.Import.Identity;
using Granit.DataExchange.Import.Reporting;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Granit.DataExchange.EntityFrameworkCore.Tests.Execution;

public sealed class EfImportExecutorTests
{
    private static string NewDb() => Guid.NewGuid().ToString();

    private static EfImportExecutor<TestEntity, TestAppDbContext> CreateExecutor(string dbName) =>
        new(new InMemoryAppContextFactory(dbName));

    private static async IAsyncEnumerable<ValidatedRow<TestEntity>> CreateInsertRows(int count)
    {
        for (int i = 0; i < count; i++)
        {
            TestEntity entity = new()
            {
                Id = Guid.NewGuid(),
                Name = $"Entity-{i}",
                Email = $"entity{i}@test.com",
            };
            yield return new ValidatedRow<TestEntity>(i + 1, entity);
        }

        await Task.CompletedTask;
    }

    [Fact]
    public async Task ExecuteAsync_inserts_all_entities()
    {
        // Arrange
        string dbName = NewDb();
        EfImportExecutor<TestEntity, TestAppDbContext> executor = CreateExecutor(dbName);
        ImportExecutionOptions options = new() { BatchSize = 100 };

        // Act
        ImportReport report = await executor.ExecuteAsync(
            CreateInsertRows(5), options, null, TestContext.Current.CancellationToken);

        // Assert
        report.TotalRows.ShouldBe(5);
        report.SucceededRows.ShouldBe(5);
        report.InsertedRows.ShouldBe(5);
        report.UpdatedRows.ShouldBe(0);
        report.FailedRows.ShouldBe(0);
        report.FinalStatus.ShouldBe(ImportJobStatus.Completed);

        // Verify in DB
        InMemoryAppContextFactory factory = new(dbName);
        await using TestAppDbContext context = factory.CreateDbContext();
        int count = await context.TestEntities.CountAsync(TestContext.Current.CancellationToken);
        count.ShouldBe(5);
    }

    [Fact]
    public async Task ExecuteAsync_batches_correctly()
    {
        // Arrange
        string dbName = NewDb();
        EfImportExecutor<TestEntity, TestAppDbContext> executor = CreateExecutor(dbName);
        ImportExecutionOptions options = new() { BatchSize = 2 };

        // Act
        ImportReport report = await executor.ExecuteAsync(
            CreateInsertRows(5), options, null, TestContext.Current.CancellationToken);

        // Assert
        report.TotalRows.ShouldBe(5);
        report.SucceededRows.ShouldBe(5);
    }

    [Fact]
    public async Task ExecuteAsync_reports_progress()
    {
        // Arrange
        string dbName = NewDb();
        EfImportExecutor<TestEntity, TestAppDbContext> executor = CreateExecutor(dbName);
        ImportExecutionOptions options = new() { BatchSize = 2 };
        List<ImportProgress> progressReports = [];
        SynchronousProgress progress = new(progressReports);

        // Act
        await executor.ExecuteAsync(
            CreateInsertRows(5),
            options,
            progress,
            TestContext.Current.CancellationToken);

        // Assert — at least one progress report should have been sent
        // (progress from batch boundaries + final)
        progressReports.Count.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// Synchronous IProgress implementation to avoid <see cref="Progress{T}"/>
    /// posting to the thread pool (which causes race conditions in tests).
    /// </summary>
    private sealed class SynchronousProgress(List<ImportProgress> reports) : IProgress<ImportProgress>
    {
        public void Report(ImportProgress value) => reports.Add(value);
    }

    [Fact]
    public async Task ExecuteAsync_updates_existing_entities()
    {
        // Arrange
        string dbName = NewDb();
        var existingId = Guid.NewGuid();

        // Seed existing entity
        InMemoryAppContextFactory factory = new(dbName);
        await using (TestAppDbContext context = factory.CreateDbContext())
        {
            context.TestEntities.Add(new TestEntity
            {
                Id = existingId,
                Name = "Original",
                Email = "original@test.com",
            });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        EfImportExecutor<TestEntity, TestAppDbContext> executor = CreateExecutor(dbName);
        ImportExecutionOptions options = new() { BatchSize = 100 };

        // Create a row with Update identity
        TestEntity existingEntity;
        await using (TestAppDbContext readContext = factory.CreateDbContext())
        {
            existingEntity = (await readContext.TestEntities.FindAsync([existingId], TestContext.Current.CancellationToken))!;
        }

        TestEntity updatedEntity = new()
        {
            Id = existingId,
            Name = "Updated",
            Email = "updated@test.com",
        };

        RecordIdentity<TestEntity> identity = new()
        {
            Operation = RecordOperation.Update,
            ExistingEntity = existingEntity,
        };

        async IAsyncEnumerable<ValidatedRow<TestEntity>> CreateUpdateRows()
        {
            yield return new ValidatedRow<TestEntity>(1, updatedEntity, identity);
            await Task.CompletedTask;
        }

        // Act
        ImportReport report = await executor.ExecuteAsync(
            CreateUpdateRows(), options, null, TestContext.Current.CancellationToken);

        // Assert
        report.UpdatedRows.ShouldBe(1);
        report.InsertedRows.ShouldBe(0);
        report.FinalStatus.ShouldBe(ImportJobStatus.Completed);
    }

    [Fact]
    public async Task ExecuteAsync_dry_run_reports_correct_counts()
    {
        // Arrange
        string dbName = NewDb();
        EfImportExecutor<TestEntity, TestAppDbContext> executor = CreateExecutor(dbName);
        ImportExecutionOptions options = new() { BatchSize = 100, DryRun = true };

        // Act
        ImportReport report = await executor.ExecuteAsync(
            CreateInsertRows(3), options, null, TestContext.Current.CancellationToken);

        // Assert — report should reflect the rows processed (dry-run validates the pipeline)
        // Note: InMemory provider does not support transactions, so rollback is a no-op.
        // In production with a real DB, the transaction would be rolled back.
        report.TotalRows.ShouldBe(3);
        report.SucceededRows.ShouldBe(3);
        report.InsertedRows.ShouldBe(3);
        report.FailedRows.ShouldBe(0);
        report.FinalStatus.ShouldBe(ImportJobStatus.Completed);
    }

    [Fact]
    public async Task ExecuteAsync_empty_stream_returns_completed()
    {
        // Arrange
        string dbName = NewDb();
        EfImportExecutor<TestEntity, TestAppDbContext> executor = CreateExecutor(dbName);
        ImportExecutionOptions options = new();

        static async IAsyncEnumerable<ValidatedRow<TestEntity>> EmptyStream()
        {
            await Task.CompletedTask;
            yield break;
        }

        // Act
        ImportReport report = await executor.ExecuteAsync(
            EmptyStream(), options, null, TestContext.Current.CancellationToken);

        // Assert
        report.TotalRows.ShouldBe(0);
        report.SucceededRows.ShouldBe(0);
        report.FinalStatus.ShouldBe(ImportJobStatus.Completed);
    }

    [Fact]
    public async Task ExecuteAsync_report_duration_is_positive()
    {
        // Arrange
        string dbName = NewDb();
        EfImportExecutor<TestEntity, TestAppDbContext> executor = CreateExecutor(dbName);
        ImportExecutionOptions options = new();

        // Act
        ImportReport report = await executor.ExecuteAsync(
            CreateInsertRows(3), options, null, TestContext.Current.CancellationToken);

        // Assert
        report.Duration.ShouldBeGreaterThan(TimeSpan.Zero);
    }
}
