using FluentAssertions;
using Granit.BackgroundJobs.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Granit.BackgroundJobs.EntityFrameworkCore.Tests;

public sealed class BackgroundJobsDbContextTests
{
    private static BackgroundJobsDbContext CreateInMemory()
    {
        DbContextOptions<BackgroundJobsDbContext> options =
            new DbContextOptionsBuilder<BackgroundJobsDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

        return new BackgroundJobsDbContext(options);
    }

    // =========================================================================
    // Schema creation
    // =========================================================================

    [Fact]
    public async Task EnsureCreatedAsync_WithInMemoryProvider_DoesNotThrow()
    {
        await using BackgroundJobsDbContext ctx = CreateInMemory();

        Func<Task> act = () => ctx.Database.EnsureCreatedAsync(
            TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
    }

    // =========================================================================
    // Entity configuration — model metadata
    // =========================================================================

    [Fact]
    public void Model_TableName_IsSchedulingBackgroundJobs()
    {
        using BackgroundJobsDbContext ctx = CreateInMemory();

        string? tableName = ctx.Model
            .FindEntityType(typeof(BackgroundJobDefinition))!
            .GetTableName();

        tableName.Should().Be("scheduling_background_jobs");
    }

    [Fact]
    public void Model_JobNameIndex_IsUnique()
    {
        using BackgroundJobsDbContext ctx = CreateInMemory();

        IEntityType entityType = ctx.Model.FindEntityType(typeof(BackgroundJobDefinition))!;

        IIndex? uniqueIndex = entityType.GetIndexes()
            .FirstOrDefault(i =>
                i.IsUnique &&
                i.Properties.Any(p => p.Name == nameof(BackgroundJobDefinition.JobName)));

        uniqueIndex.Should().NotBeNull("a unique index on JobName must be configured");
    }

    [Fact]
    public void Model_JobName_HasMaxLength200()
    {
        using BackgroundJobsDbContext ctx = CreateInMemory();

        IProperty? property = ctx.Model
            .FindEntityType(typeof(BackgroundJobDefinition))!
            .FindProperty(nameof(BackgroundJobDefinition.JobName));

        property!.GetMaxLength().Should().Be(200);
        property.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void Model_MessageType_HasMaxLength500()
    {
        using BackgroundJobsDbContext ctx = CreateInMemory();

        IProperty? property = ctx.Model
            .FindEntityType(typeof(BackgroundJobDefinition))!
            .FindProperty(nameof(BackgroundJobDefinition.MessageType));

        property!.GetMaxLength().Should().Be(500);
        property.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void Model_CronExpression_HasMaxLength100()
    {
        using BackgroundJobsDbContext ctx = CreateInMemory();

        IProperty? property = ctx.Model
            .FindEntityType(typeof(BackgroundJobDefinition))!
            .FindProperty(nameof(BackgroundJobDefinition.CronExpression));

        property!.GetMaxLength().Should().Be(100);
        property.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void Model_LastErrorMessage_HasMaxLength2000()
    {
        using BackgroundJobsDbContext ctx = CreateInMemory();

        IProperty? property = ctx.Model
            .FindEntityType(typeof(BackgroundJobDefinition))!
            .FindProperty(nameof(BackgroundJobDefinition.LastErrorMessage));

        property!.GetMaxLength().Should().Be(2000);
        property.IsNullable.Should().BeTrue();
    }

    [Fact]
    public void Model_TriggeredBy_HasMaxLength450()
    {
        using BackgroundJobsDbContext ctx = CreateInMemory();

        IProperty? property = ctx.Model
            .FindEntityType(typeof(BackgroundJobDefinition))!
            .FindProperty(nameof(BackgroundJobDefinition.TriggeredBy));

        property!.GetMaxLength().Should().Be(450);
        property.IsNullable.Should().BeTrue();
    }

    // =========================================================================
    // CRUD roundtrip
    // =========================================================================

    [Fact]
    public async Task SaveAndReload_AllFields_MatchOriginal()
    {
        await using BackgroundJobsDbContext ctx = CreateInMemory();

        BackgroundJobDefinition job = new()
        {
            Id = Guid.NewGuid(),
            JobName = "daily-report",
            MessageType = "My.App.DailyReportMessage, My.App",
            CronExpression = "0 8 * * *",
            IsEnabled = true,
            LastExecutedAt = new DateTimeOffset(2026, 1, 15, 8, 0, 0, TimeSpan.Zero),
            NextExecutionAt = new DateTimeOffset(2026, 1, 16, 8, 0, 0, TimeSpan.Zero),
            ConsecutiveFailureCount = 2,
            LastErrorMessage = "Timeout after 30s",
            TriggeredBy = "admin-user",
        };

        ctx.Jobs.Add(job);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        ctx.ChangeTracker.Clear();

        BackgroundJobDefinition? loaded = await ctx.Jobs
            .FindAsync([job.Id], TestContext.Current.CancellationToken);

        loaded.Should().NotBeNull();
        loaded!.JobName.Should().Be("daily-report");
        loaded.MessageType.Should().Be("My.App.DailyReportMessage, My.App");
        loaded.CronExpression.Should().Be("0 8 * * *");
        loaded.IsEnabled.Should().BeTrue();
        loaded.LastExecutedAt.Should().Be(job.LastExecutedAt);
        loaded.NextExecutionAt.Should().Be(job.NextExecutionAt);
        loaded.ConsecutiveFailureCount.Should().Be(2);
        loaded.LastErrorMessage.Should().Be("Timeout after 30s");
        loaded.TriggeredBy.Should().Be("admin-user");
    }
}
