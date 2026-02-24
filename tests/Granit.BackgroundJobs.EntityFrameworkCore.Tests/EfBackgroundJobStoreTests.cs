using FluentAssertions;
using Granit.BackgroundJobs.EntityFrameworkCore.Internal;
using Granit.BackgroundJobs.Internal;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Granit.BackgroundJobs.EntityFrameworkCore.Tests;

public sealed class EfBackgroundJobStoreTests
{
    // =========================================================================
    // Test infrastructure
    // =========================================================================

    private sealed class InMemoryContextFactory(string dbName) : IDbContextFactory<BackgroundJobsDbContext>
    {
        public BackgroundJobsDbContext CreateDbContext()
        {
            DbContextOptions<BackgroundJobsDbContext> options =
                new DbContextOptionsBuilder<BackgroundJobsDbContext>()
                    .UseInMemoryDatabase(dbName)
                    .Options;
            return new BackgroundJobsDbContext(options);
        }

        public Task<BackgroundJobsDbContext> CreateDbContextAsync(CancellationToken ct = default) =>
            Task.FromResult(CreateDbContext());
    }

    private static EfBackgroundJobStore CreateStore(string dbName) =>
        new(new InMemoryContextFactory(dbName));

    private static RecurringJobRegistration MakeRegistration(
        string jobName = "test-job",
        string cron = "0 * * * *",
        string messageType = "My.App.TestMessage, My.App") =>
        new(jobName, cron, messageType);

    private static async Task<EfBackgroundJobStore> SeedAsync(
        string dbName,
        string jobName = "test-job",
        CancellationToken ct = default)
    {
        EfBackgroundJobStore store = CreateStore(dbName);
        await store.SeedJobsAsync([MakeRegistration(jobName)], ct);
        return store;
    }

    // =========================================================================
    // FindAsync
    // =========================================================================

    [Fact]
    public async Task FindAsync_UnknownJob_ReturnsNull()
    {
        EfBackgroundJobStore store = CreateStore(Guid.NewGuid().ToString());

        BackgroundJobDefinition? result = await store.FindAsync(
            "non-existent", TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_KnownJob_ReturnsDefinition()
    {
        string db = Guid.NewGuid().ToString();
        EfBackgroundJobStore store = await SeedAsync(db, ct: TestContext.Current.CancellationToken);

        BackgroundJobDefinition? result = await store.FindAsync(
            "test-job", TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.JobName.Should().Be("test-job");
    }

    // =========================================================================
    // SeedJobsAsync
    // =========================================================================

    [Fact]
    public async Task SeedJobsAsync_NewJob_IsInsertedWithDefaultsEnabled()
    {
        string db = Guid.NewGuid().ToString();
        EfBackgroundJobStore store = CreateStore(db);

        await store.SeedJobsAsync([MakeRegistration()], TestContext.Current.CancellationToken);

        BackgroundJobDefinition? job = await store.FindAsync(
            "test-job", TestContext.Current.CancellationToken);
        job.Should().NotBeNull();
        job!.IsEnabled.Should().BeTrue();
        job.ConsecutiveFailureCount.Should().Be(0);
    }

    [Fact]
    public async Task SeedJobsAsync_ExistingJob_PreservesAdminStateAndUpdatesCron()
    {
        string db = Guid.NewGuid().ToString();
        EfBackgroundJobStore store = await SeedAsync(db, ct: TestContext.Current.CancellationToken);
        await store.SetEnabledAsync("test-job", false, TestContext.Current.CancellationToken);

        await store.SeedJobsAsync(
            [MakeRegistration(cron: "0 8 * * *")], TestContext.Current.CancellationToken);

        BackgroundJobDefinition? job = await store.FindAsync(
            "test-job", TestContext.Current.CancellationToken);
        job!.IsEnabled.Should().BeFalse("administrative state must be preserved");
        job.CronExpression.Should().Be("0 8 * * *", "cron expression must be updated");
    }

    [Fact]
    public async Task SeedJobsAsync_CalledTwice_IsIdempotent()
    {
        string db = Guid.NewGuid().ToString();
        EfBackgroundJobStore store = CreateStore(db);

        await store.SeedJobsAsync([MakeRegistration()], TestContext.Current.CancellationToken);
        await store.SeedJobsAsync([MakeRegistration()], TestContext.Current.CancellationToken);

        IReadOnlyList<BackgroundJobDefinition> all = await store.GetAllJobsAsync(
            TestContext.Current.CancellationToken);
        all.Should().HaveCount(1);
    }

    // =========================================================================
    // SetEnabledAsync
    // =========================================================================

    [Fact]
    public async Task SetEnabledAsync_ToFalse_PersistsValue()
    {
        string db = Guid.NewGuid().ToString();
        EfBackgroundJobStore store = await SeedAsync(db, ct: TestContext.Current.CancellationToken);

        await store.SetEnabledAsync("test-job", false, TestContext.Current.CancellationToken);

        BackgroundJobDefinition? job = await store.FindAsync(
            "test-job", TestContext.Current.CancellationToken);
        job!.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task SetEnabledAsync_UnknownJob_DoesNotThrow()
    {
        EfBackgroundJobStore store = CreateStore(Guid.NewGuid().ToString());

        Func<Task> act = () => store.SetEnabledAsync(
            "ghost", false, TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
    }

    // =========================================================================
    // RecordExecutionStartAsync
    // =========================================================================

    [Fact]
    public async Task RecordExecutionStartAsync_ResetsCountersAndSetsTimestamp()
    {
        string db = Guid.NewGuid().ToString();
        EfBackgroundJobStore store = await SeedAsync(db, ct: TestContext.Current.CancellationToken);
        await store.RecordExecutionFailureAsync(
            "test-job", "previous error", TestContext.Current.CancellationToken);

        DateTimeOffset startedAt = new(2026, 2, 20, 8, 0, 0, TimeSpan.Zero);
        await store.RecordExecutionStartAsync(
            "test-job", startedAt, TestContext.Current.CancellationToken);

        BackgroundJobDefinition? job = await store.FindAsync(
            "test-job", TestContext.Current.CancellationToken);
        job!.LastExecutedAt.Should().Be(startedAt);
        job.ConsecutiveFailureCount.Should().Be(0);
        job.LastErrorMessage.Should().BeNull();
        job.TriggeredBy.Should().BeNull();
    }

    // =========================================================================
    // RecordNextExecutionAsync
    // =========================================================================

    [Fact]
    public async Task RecordNextExecutionAsync_PersistsNextOccurrence()
    {
        string db = Guid.NewGuid().ToString();
        EfBackgroundJobStore store = await SeedAsync(db, ct: TestContext.Current.CancellationToken);

        DateTimeOffset next = new(2026, 2, 21, 8, 0, 0, TimeSpan.Zero);
        await store.RecordNextExecutionAsync(
            "test-job", next, TestContext.Current.CancellationToken);

        BackgroundJobDefinition? job = await store.FindAsync(
            "test-job", TestContext.Current.CancellationToken);
        job!.NextExecutionAt.Should().Be(next);
    }

    // =========================================================================
    // RecordExecutionFailureAsync
    // =========================================================================

    [Fact]
    public async Task RecordExecutionFailureAsync_IncrementsCounterAndStoresMessage()
    {
        string db = Guid.NewGuid().ToString();
        EfBackgroundJobStore store = await SeedAsync(db, ct: TestContext.Current.CancellationToken);
        await store.RecordExecutionFailureAsync(
            "test-job", "first error", TestContext.Current.CancellationToken);

        await store.RecordExecutionFailureAsync(
            "test-job", "timeout", TestContext.Current.CancellationToken);

        BackgroundJobDefinition? job = await store.FindAsync(
            "test-job", TestContext.Current.CancellationToken);
        job!.ConsecutiveFailureCount.Should().Be(2);
        job.LastErrorMessage.Should().Be("timeout");
    }

    [Fact]
    public async Task RecordExecutionFailureAsync_UnknownJob_DoesNotThrow()
    {
        EfBackgroundJobStore store = CreateStore(Guid.NewGuid().ToString());

        Func<Task> act = () => store.RecordExecutionFailureAsync(
            "ghost", "err", TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
    }

    // =========================================================================
    // GetEnabledJobsAsync / GetAllJobsAsync
    // =========================================================================

    [Fact]
    public async Task GetEnabledJobsAsync_ReturnsOnlyEnabledJobs()
    {
        string db = Guid.NewGuid().ToString();
        EfBackgroundJobStore store = CreateStore(db);
        await store.SeedJobsAsync(
            [MakeRegistration("job-a"), MakeRegistration("job-b")],
            TestContext.Current.CancellationToken);
        await store.SetEnabledAsync("job-b", false, TestContext.Current.CancellationToken);

        IReadOnlyList<BackgroundJobDefinition> enabled = await store.GetEnabledJobsAsync(
            TestContext.Current.CancellationToken);

        enabled.Should().HaveCount(1);
        enabled[0].JobName.Should().Be("job-a");
    }

    [Fact]
    public async Task GetAllJobsAsync_ReturnsAllJobsRegardlessOfEnabled()
    {
        string db = Guid.NewGuid().ToString();
        EfBackgroundJobStore store = CreateStore(db);
        await store.SeedJobsAsync(
            [MakeRegistration("job-a"), MakeRegistration("job-b")],
            TestContext.Current.CancellationToken);
        await store.SetEnabledAsync("job-b", false, TestContext.Current.CancellationToken);

        IReadOnlyList<BackgroundJobDefinition> all = await store.GetAllJobsAsync(
            TestContext.Current.CancellationToken);

        all.Should().HaveCount(2);
    }

    // =========================================================================
    // SetTriggeredByAsync
    // =========================================================================

    [Fact]
    public async Task SetTriggeredByAsync_PersistsOperatorId()
    {
        string db = Guid.NewGuid().ToString();
        EfBackgroundJobStore store = await SeedAsync(db, ct: TestContext.Current.CancellationToken);

        await store.SetTriggeredByAsync(
            "test-job", "operator-42", TestContext.Current.CancellationToken);

        BackgroundJobDefinition? job = await store.FindAsync(
            "test-job", TestContext.Current.CancellationToken);
        job!.TriggeredBy.Should().Be("operator-42");
    }

    [Fact]
    public async Task SetTriggeredByAsync_Null_ClearsField()
    {
        string db = Guid.NewGuid().ToString();
        EfBackgroundJobStore store = await SeedAsync(db, ct: TestContext.Current.CancellationToken);
        await store.SetTriggeredByAsync(
            "test-job", "operator-42", TestContext.Current.CancellationToken);

        await store.SetTriggeredByAsync(
            "test-job", null, TestContext.Current.CancellationToken);

        BackgroundJobDefinition? job = await store.FindAsync(
            "test-job", TestContext.Current.CancellationToken);
        job!.TriggeredBy.Should().BeNull();
    }
}
