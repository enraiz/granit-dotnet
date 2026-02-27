using FluentAssertions;
using Granit.BackgroundJobs.Internal;
using Granit.Core.Exceptions;
using JasperFx.Core;
using Granit.Security;
using Granit.Timing;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Wolverine;
using Wolverine.Persistence.Durability;
using Wolverine.Persistence.Durability.DeadLetterManagement;
using Xunit;

namespace Granit.BackgroundJobs.Tests;

public sealed class BackgroundJobManagerTests
{
    private readonly IBackgroundJobStore _store = Substitute.For<IBackgroundJobStore>();
    private readonly IMessageBus _bus = Substitute.For<IMessageBus>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly ICurrentUserService _user = Substitute.For<ICurrentUserService>();
    private readonly ILogger<BackgroundJobManager> _logger =
        Substitute.For<ILogger<BackgroundJobManager>>();
    private readonly IMessageStore _messageStore = Substitute.For<IMessageStore>();
    private readonly IDeadLetters _deadLetters = Substitute.For<IDeadLetters>();

    public BackgroundJobManagerTests()
    {
        // Allow [LoggerMessage] generated code to execute both branches (IsEnabled check)
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        // Default: no dead letters (graceful baseline)
        _messageStore.DeadLetters.Returns(_deadLetters);
        _deadLetters
            .SummarizeAllAsync(Arg.Any<string>(), Arg.Any<TimeRange>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<DeadLetterQueueCount>>([]));
    }

    private BackgroundJobManager MakeSut() =>
        new(_store, _bus, _clock, _user, _logger, _messageStore);

    private static BackgroundJobDefinition MakeJob(
        string name = "test-job",
        string cron = "0 * * * *",
        bool enabled = true) =>
        new()
        {
            Id = Guid.NewGuid(),
            JobName = name,
            CronExpression = cron,
            MessageType = typeof(FakeDailyReportMessage).AssemblyQualifiedName!,
            IsEnabled = enabled,
        };

    // =========================================================================
    // GetAllAsync
    // =========================================================================

    [Fact]
    public async Task GetAllAsync_ReturnsStatusForEachJob()
    {
        // Arrange
        BackgroundJobDefinition job = MakeJob("daily-report", "0 8 * * *");
        _store.GetAllJobsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<BackgroundJobDefinition>>([job]));

        BackgroundJobManager sut = MakeSut();
        CancellationToken ct = TestContext.Current.CancellationToken;

        // Act
        IReadOnlyList<BackgroundJobStatus> result = await sut.GetAllAsync(ct);

        // Assert
        result.Should().HaveCount(1);
        result[0].JobName.Should().Be("daily-report");
        result[0].CronExpression.Should().Be("0 8 * * *");
        result[0].IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllAsync_WithDlqEntries_PopulatesDeadLetterCount()
    {
        // Arrange
        string messageTypeShortName =
            typeof(FakeDailyReportMessage).AssemblyQualifiedName!.Split(',')[0].Trim();

        BackgroundJobDefinition job = MakeJob("daily-report", "0 8 * * *");
        job.ConsecutiveFailureCount = 3;
        job.LastErrorMessage = "timeout";
        _store.GetAllJobsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<BackgroundJobDefinition>>([job]));

        DeadLetterQueueCount dlqEntry = new(
            ServiceName: "my-app",
            ReceivedAt: new Uri("queue://test"),
            MessageType: messageTypeShortName,
            ExceptionType: "TimeoutException",
            Database: new Uri("db://test"),
            Count: 5);

        _deadLetters
            .SummarizeAllAsync(Arg.Any<string>(), Arg.Any<TimeRange>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<DeadLetterQueueCount>>([dlqEntry]));

        BackgroundJobManager sut = MakeSut();

        // Act
        IReadOnlyList<BackgroundJobStatus> result =
            await sut.GetAllAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(1);
        result[0].ConsecutiveFailures.Should().Be(3);
        result[0].LastError.Should().Be("timeout");
        result[0].DeadLetterCount.Should().Be(5);
    }

    [Fact]
    public async Task GetAllAsync_WhenDlqQueryThrows_ReturnsZeroDeadLetterCount()
    {
        // Arrange
        BackgroundJobDefinition job = MakeJob("daily-report");
        _store.GetAllJobsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<BackgroundJobDefinition>>([job]));

        _deadLetters
            .SummarizeAllAsync(Arg.Any<string>(), Arg.Any<TimeRange>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("DLQ unavailable"));

        BackgroundJobManager sut = MakeSut();

        // Act — must not throw
        IReadOnlyList<BackgroundJobStatus> result =
            await sut.GetAllAsync(TestContext.Current.CancellationToken);

        // Assert — graceful degradation
        result.Should().HaveCount(1);
        result[0].DeadLetterCount.Should().Be(0);
    }

    // =========================================================================
    // FindAsync
    // =========================================================================

    [Fact]
    public async Task FindAsync_KnownJob_ReturnsStatus()
    {
        // Arrange
        BackgroundJobDefinition job = MakeJob("daily-report");
        _store.FindAsync("daily-report", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BackgroundJobDefinition?>(job));

        BackgroundJobManager sut = MakeSut();

        // Act
        BackgroundJobStatus? status =
            await sut.FindAsync("daily-report", TestContext.Current.CancellationToken);

        // Assert
        status.Should().NotBeNull();
        status!.JobName.Should().Be("daily-report");
    }

    [Fact]
    public async Task FindAsync_UnknownJob_ReturnsNull()
    {
        // Arrange
        _store.FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BackgroundJobDefinition?>(null));

        BackgroundJobManager sut = MakeSut();

        // Act
        BackgroundJobStatus? status =
            await sut.FindAsync("ghost", TestContext.Current.CancellationToken);

        // Assert
        status.Should().BeNull();
    }

    // =========================================================================
    // PauseAsync
    // =========================================================================

    [Fact]
    public async Task PauseAsync_KnownJob_SetsEnabledFalse()
    {
        // Arrange
        BackgroundJobDefinition job = MakeJob("daily-report");
        _store.FindAsync("daily-report", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BackgroundJobDefinition?>(job));

        BackgroundJobManager sut = MakeSut();
        CancellationToken ct = TestContext.Current.CancellationToken;

        // Act
        await sut.PauseAsync("daily-report", ct);

        // Assert
        await _store.Received(1).SetEnabledAsync("daily-report", false, ct);
    }

    [Fact]
    public async Task PauseAsync_UnknownJob_ThrowsEntityNotFoundException()
    {
        // Arrange
        _store.FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BackgroundJobDefinition?>(null));

        BackgroundJobManager sut = MakeSut();

        // Act
        Func<Task> act = () => sut.PauseAsync("ghost", TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    // =========================================================================
    // ResumeAsync
    // =========================================================================

    [Fact]
    public async Task ResumeAsync_KnownJob_SetsEnabledTrueAndSchedules()
    {
        // Arrange
        BackgroundJobDefinition job = MakeJob("daily-report", "0 8 * * *");
        _store.FindAsync("daily-report", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BackgroundJobDefinition?>(job));
        _clock.Now.Returns(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        BackgroundJobManager sut = MakeSut();
        CancellationToken ct = TestContext.Current.CancellationToken;

        // Act
        await sut.ResumeAsync("daily-report", ct);

        // Assert
        await _store.Received(1).SetEnabledAsync("daily-report", true, ct);
        await _store.Received(1).RecordNextExecutionAsync(
            "daily-report", Arg.Any<DateTimeOffset>(), ct);
    }

    [Fact]
    public async Task ResumeAsync_UnknownJob_ThrowsEntityNotFoundException()
    {
        // Arrange
        _store.FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BackgroundJobDefinition?>(null));

        BackgroundJobManager sut = MakeSut();

        // Act
        Func<Task> act = () => sut.ResumeAsync("ghost", TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task ResumeAsync_InvalidCron_LogsWarningAndSkipsScheduling()
    {
        // Arrange — cron expression that produces no next occurrence (unreachable)
        BackgroundJobDefinition job = MakeJob("daily-report", "0 8 31 2 *"); // Feb 31 never exists
        _store.FindAsync("daily-report", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BackgroundJobDefinition?>(job));
        _clock.Now.Returns(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        BackgroundJobManager sut = MakeSut();

        // Act — should not throw, just log warning
        Func<Task> act = () =>
            sut.ResumeAsync("daily-report", TestContext.Current.CancellationToken);

        // Assert
        await act.Should().NotThrowAsync();
        await _store.DidNotReceive()
            .RecordNextExecutionAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>());
    }

    // =========================================================================
    // TriggerNowAsync
    // =========================================================================

    [Fact]
    public async Task TriggerNowAsync_AuthenticatedUser_PublishesWithTriggeredByHeader()
    {
        // Arrange
        BackgroundJobDefinition job = MakeJob("daily-report");
        _store.FindAsync("daily-report", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BackgroundJobDefinition?>(job));
        _user.IsAuthenticated.Returns(true);
        _user.UserId.Returns("user-abc");

        BackgroundJobManager sut = MakeSut();
        CancellationToken ct = TestContext.Current.CancellationToken;

        // Act
        await sut.TriggerNowAsync("daily-report", ct);

        // Assert
        await _bus.Received(1).PublishAsync(
            Arg.Is<FakeDailyReportMessage>(m => m != null),
            Arg.Is<DeliveryOptions>(o =>
                o.Headers.ContainsKey(RecurringJobSchedulingMiddleware.TriggeredByHeader)
                && o.Headers[RecurringJobSchedulingMiddleware.TriggeredByHeader] == "user-abc"));
    }

    [Fact]
    public async Task TriggerNowAsync_AnonymousUser_PublishesWithoutTriggeredByHeader()
    {
        // Arrange
        BackgroundJobDefinition job = MakeJob("daily-report");
        _store.FindAsync("daily-report", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BackgroundJobDefinition?>(job));
        _user.IsAuthenticated.Returns(false);

        BackgroundJobManager sut = MakeSut();

        // Act
        await sut.TriggerNowAsync("daily-report", TestContext.Current.CancellationToken);

        // Assert
        await _bus.Received(1).PublishAsync(
            Arg.Any<FakeDailyReportMessage>(),
            Arg.Is<DeliveryOptions>(o =>
                !o.Headers.ContainsKey(RecurringJobSchedulingMiddleware.TriggeredByHeader)));
    }

    [Fact]
    public async Task TriggerNowAsync_UnknownJob_ThrowsEntityNotFoundException()
    {
        // Arrange
        _store.FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BackgroundJobDefinition?>(null));

        BackgroundJobManager sut = MakeSut();

        // Act
        Func<Task> act = () =>
            sut.TriggerNowAsync("ghost", TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<EntityNotFoundException>();
    }
}
