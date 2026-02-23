using FluentAssertions;
using Granit.BackgroundJobs.Internal;
using Xunit;

namespace Granit.BackgroundJobs.Tests;

public sealed class InMemoryBackgroundJobStoreTests
{
    private readonly InMemoryBackgroundJobStore _sut = new();
    private readonly DateTimeOffset _now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private static RecurringJobRegistration MakeRegistration(
        string name = "test-job",
        string cron = "0 * * * *",
        string messageType = "Granit.BackgroundJobs.Tests.FakeMessage, Granit.BackgroundJobs.Tests") =>
        new(JobName: name, CronExpression: cron, MessageType: messageType);

    // =========================================================================
    // SeedJobsAsync
    // =========================================================================

    [Fact]
    public async Task SeedJobsAsync_NewJob_AddsItToStore()
    {
        // Arrange
        RecurringJobRegistration reg = MakeRegistration("daily-report", "0 8 * * *");
        CancellationToken ct = TestContext.Current.CancellationToken;

        // Act
        await _sut.SeedJobsAsync([reg], ct);
        BackgroundJobDefinition? job = await _sut.FindAsync("daily-report", ct);

        // Assert
        job.Should().NotBeNull();
        job!.JobName.Should().Be("daily-report");
        job.CronExpression.Should().Be("0 8 * * *");
        job.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task SeedJobsAsync_ExistingJob_PreservesIsEnabledAndUpdatesCron()
    {
        // Arrange — seed first, then pause it manually
        RecurringJobRegistration reg = MakeRegistration("daily-report", "0 8 * * *");
        CancellationToken ct = TestContext.Current.CancellationToken;
        await _sut.SeedJobsAsync([reg], ct);
        await _sut.SetEnabledAsync("daily-report", false, ct);

        // Act — re-seed with updated cron expression
        RecurringJobRegistration updated = MakeRegistration("daily-report", "0 9 * * *");
        await _sut.SeedJobsAsync([updated], ct);
        BackgroundJobDefinition? job = await _sut.FindAsync("daily-report", ct);

        // Assert — cron updated, pause state preserved
        job!.CronExpression.Should().Be("0 9 * * *");
        job.IsEnabled.Should().BeFalse();
    }

    // =========================================================================
    // FindAsync / GetAllJobsAsync / GetEnabledJobsAsync
    // =========================================================================

    [Fact]
    public async Task FindAsync_UnknownJob_ReturnsNull()
    {
        // Act
        BackgroundJobDefinition? result =
            await _sut.FindAsync("does-not-exist", TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllJobsAsync_MultipleJobs_ReturnsAll()
    {
        // Arrange
        CancellationToken ct = TestContext.Current.CancellationToken;
        await _sut.SeedJobsAsync([MakeRegistration("job-a"), MakeRegistration("job-b")], ct);

        // Act
        IReadOnlyList<BackgroundJobDefinition> jobs = await _sut.GetAllJobsAsync(ct);

        // Assert
        jobs.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetEnabledJobsAsync_FiltersDisabledJobs()
    {
        // Arrange
        CancellationToken ct = TestContext.Current.CancellationToken;
        await _sut.SeedJobsAsync([MakeRegistration("job-a"), MakeRegistration("job-b")], ct);
        await _sut.SetEnabledAsync("job-a", false, ct);

        // Act
        IReadOnlyList<BackgroundJobDefinition> enabled = await _sut.GetEnabledJobsAsync(ct);

        // Assert
        enabled.Should().HaveCount(1);
        enabled.Single().JobName.Should().Be("job-b");
    }

    // =========================================================================
    // Execution tracking
    // =========================================================================

    [Fact]
    public async Task RecordExecutionStartAsync_UpdatesLastExecutedAtAndClearsErrors()
    {
        // Arrange
        CancellationToken ct = TestContext.Current.CancellationToken;
        await _sut.SeedJobsAsync([MakeRegistration("test-job")], ct);
        await _sut.RecordExecutionFailureAsync("test-job", "previous error", ct);

        // Act
        await _sut.RecordExecutionStartAsync("test-job", _now, ct);
        BackgroundJobDefinition? job = await _sut.FindAsync("test-job", ct);

        // Assert
        job!.LastExecutedAt.Should().Be(_now);
        job.LastErrorMessage.Should().BeNull();
        job.ConsecutiveFailureCount.Should().Be(0);
        job.TriggeredBy.Should().BeNull();
    }

    [Fact]
    public async Task RecordNextExecutionAsync_UpdatesNextExecutionAt()
    {
        // Arrange
        CancellationToken ct = TestContext.Current.CancellationToken;
        await _sut.SeedJobsAsync([MakeRegistration("test-job")], ct);
        DateTimeOffset nextRun = _now.AddHours(1);

        // Act
        await _sut.RecordNextExecutionAsync("test-job", nextRun, ct);
        BackgroundJobDefinition? job = await _sut.FindAsync("test-job", ct);

        // Assert
        job!.NextExecutionAt.Should().Be(nextRun);
    }

    [Fact]
    public async Task RecordExecutionFailureAsync_IncrementsConsecutiveFailureCount()
    {
        // Arrange
        CancellationToken ct = TestContext.Current.CancellationToken;
        await _sut.SeedJobsAsync([MakeRegistration("test-job")], ct);

        // Act
        await _sut.RecordExecutionFailureAsync("test-job", "timeout", ct);
        await _sut.RecordExecutionFailureAsync("test-job", "timeout again", ct);
        BackgroundJobDefinition? job = await _sut.FindAsync("test-job", ct);

        // Assert
        job!.ConsecutiveFailureCount.Should().Be(2);
        job.LastErrorMessage.Should().Be("timeout again");
    }

    // =========================================================================
    // Pause / Resume / TriggeredBy
    // =========================================================================

    [Fact]
    public async Task SetEnabledAsync_PausesAndResumesJob()
    {
        // Arrange
        CancellationToken ct = TestContext.Current.CancellationToken;
        await _sut.SeedJobsAsync([MakeRegistration("test-job")], ct);

        // Act — pause, assert immediately (store returns same object reference)
        await _sut.SetEnabledAsync("test-job", false, ct);
        BackgroundJobDefinition? paused = await _sut.FindAsync("test-job", ct);
        paused!.IsEnabled.Should().BeFalse();

        // Act — resume, assert
        await _sut.SetEnabledAsync("test-job", true, ct);
        BackgroundJobDefinition? resumed = await _sut.FindAsync("test-job", ct);
        resumed!.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task SetTriggeredByAsync_SetsTriggeredBy()
    {
        // Arrange
        CancellationToken ct = TestContext.Current.CancellationToken;
        await _sut.SeedJobsAsync([MakeRegistration("test-job")], ct);

        // Act
        await _sut.SetTriggeredByAsync("test-job", "user-123", ct);
        BackgroundJobDefinition? job = await _sut.FindAsync("test-job", ct);

        // Assert
        job!.TriggeredBy.Should().Be("user-123");
    }

    [Fact]
    public async Task RecordExecutionStartAsync_UnknownJob_DoesNotThrow()
    {
        // Act — should silently ignore unknown job
        Func<Task> act = () => _sut.RecordExecutionStartAsync(
            "ghost-job", _now, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
