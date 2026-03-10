using Granit.BackgroundJobs.Abstractions;
using Granit.BackgroundJobs.Domain;
using Granit.BackgroundJobs.Internal;
using Granit.Core.Exceptions;
using Granit.Security;
using Granit.Timing;
using JasperFx.Core;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Wolverine;
using Wolverine.Persistence.Durability;
using Wolverine.Persistence.Durability.DeadLetterManagement;
using Xunit;

namespace Granit.BackgroundJobs.Tests;

public sealed class BackgroundJobManagerTests
{
    private readonly IBackgroundJobStoreReader _storeReader = Substitute.For<IBackgroundJobStoreReader>();
    private readonly IBackgroundJobStoreWriter _storeWriter = Substitute.For<IBackgroundJobStoreWriter>();
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
        new(_storeReader, _storeWriter, _bus, _clock, _user, _logger, _messageStore);

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
        _storeReader.GetAllJobsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<BackgroundJobDefinition>>([job]));

        BackgroundJobManager sut = MakeSut();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        // Act
        IReadOnlyList<BackgroundJobStatus> result = await sut.GetAllAsync(cancellationToken);

        // Assert
        result.Count.ShouldBe(1);
        result[0].JobName.ShouldBe("daily-report");
        result[0].CronExpression.ShouldBe("0 8 * * *");
        result[0].IsEnabled.ShouldBeTrue();
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
        _storeReader.GetAllJobsAsync(Arg.Any<CancellationToken>())
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
        result.Count.ShouldBe(1);
        result[0].ConsecutiveFailures.ShouldBe(3);
        result[0].LastError.ShouldBe("timeout");
        result[0].DeadLetterCount.ShouldBe(5);
    }

    [Fact]
    public async Task GetAllAsync_WhenDlqQueryThrows_ReturnsZeroDeadLetterCount()
    {
        // Arrange
        BackgroundJobDefinition job = MakeJob("daily-report");
        _storeReader.GetAllJobsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<BackgroundJobDefinition>>([job]));

        _deadLetters
            .SummarizeAllAsync(Arg.Any<string>(), Arg.Any<TimeRange>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("DLQ unavailable"));

        BackgroundJobManager sut = MakeSut();

        // Act — must not throw
        IReadOnlyList<BackgroundJobStatus> result =
            await sut.GetAllAsync(TestContext.Current.CancellationToken);

        // Assert — graceful degradation
        result.Count.ShouldBe(1);
        result[0].DeadLetterCount.ShouldBe(0);
    }

    // =========================================================================
    // FindAsync
    // =========================================================================

    [Fact]
    public async Task FindAsync_KnownJob_ReturnsStatus()
    {
        // Arrange
        BackgroundJobDefinition job = MakeJob("daily-report");
        _storeReader.FindAsync("daily-report", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BackgroundJobDefinition?>(job));

        BackgroundJobManager sut = MakeSut();

        // Act
        BackgroundJobStatus? status =
            await sut.FindAsync("daily-report", TestContext.Current.CancellationToken);

        // Assert
        status.ShouldNotBeNull();
        status!.JobName.ShouldBe("daily-report");
    }

    [Fact]
    public async Task FindAsync_UnknownJob_ReturnsNull()
    {
        // Arrange
        _storeReader.FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BackgroundJobDefinition?>(null));

        BackgroundJobManager sut = MakeSut();

        // Act
        BackgroundJobStatus? status =
            await sut.FindAsync("ghost", TestContext.Current.CancellationToken);

        // Assert
        status.ShouldBeNull();
    }

    // =========================================================================
    // PauseAsync
    // =========================================================================

    [Fact]
    public async Task PauseAsync_KnownJob_SetsEnabledFalse()
    {
        // Arrange
        BackgroundJobDefinition job = MakeJob("daily-report");
        _storeReader.FindAsync("daily-report", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BackgroundJobDefinition?>(job));

        BackgroundJobManager sut = MakeSut();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        // Act
        await sut.PauseAsync("daily-report", cancellationToken);

        // Assert
        await _storeWriter.Received(1).SetEnabledAsync("daily-report", false, cancellationToken);
    }

    [Fact]
    public async Task PauseAsync_UnknownJob_ThrowsEntityNotFoundException()
    {
        // Arrange
        _storeReader.FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BackgroundJobDefinition?>(null));

        BackgroundJobManager sut = MakeSut();

        // Act
        Func<Task> act = () => sut.PauseAsync("ghost", TestContext.Current.CancellationToken);

        // Assert
        await Should.ThrowAsync<EntityNotFoundException>(act);
    }

    // =========================================================================
    // ResumeAsync
    // =========================================================================

    [Fact]
    public async Task ResumeAsync_KnownJob_SetsEnabledTrueAndSchedules()
    {
        // Arrange
        BackgroundJobDefinition job = MakeJob("daily-report", "0 8 * * *");
        _storeReader.FindAsync("daily-report", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BackgroundJobDefinition?>(job));
        _clock.Now.Returns(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        BackgroundJobManager sut = MakeSut();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        // Act
        await sut.ResumeAsync("daily-report", cancellationToken);

        // Assert
        await _storeWriter.Received(1).SetEnabledAsync("daily-report", true, cancellationToken);
        await _storeWriter.Received(1).RecordNextExecutionAsync(
            "daily-report", Arg.Any<DateTimeOffset>(), cancellationToken);
    }

    [Fact]
    public async Task ResumeAsync_UnknownJob_ThrowsEntityNotFoundException()
    {
        // Arrange
        _storeReader.FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BackgroundJobDefinition?>(null));

        BackgroundJobManager sut = MakeSut();

        // Act
        Func<Task> act = () => sut.ResumeAsync("ghost", TestContext.Current.CancellationToken);

        // Assert
        await Should.ThrowAsync<EntityNotFoundException>(act);
    }

    [Fact]
    public async Task ResumeAsync_InvalidCron_LogsWarningAndSkipsScheduling()
    {
        // Arrange — cron expression that produces no next occurrence (unreachable)
        BackgroundJobDefinition job = MakeJob("daily-report", "0 8 31 2 *"); // Feb 31 never exists
        _storeReader.FindAsync("daily-report", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BackgroundJobDefinition?>(job));
        _clock.Now.Returns(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        BackgroundJobManager sut = MakeSut();

        // Act — should not throw, just log warning
        Func<Task> act = () =>
            sut.ResumeAsync("daily-report", TestContext.Current.CancellationToken);

        // Assert
        await Should.NotThrowAsync(act);
        await _storeWriter.DidNotReceive()
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
        _storeReader.FindAsync("daily-report", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BackgroundJobDefinition?>(job));
        _user.IsAuthenticated.Returns(true);
        _user.UserId.Returns("user-abc");

        BackgroundJobManager sut = MakeSut();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        // Act
        await sut.TriggerNowAsync("daily-report", cancellationToken);

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
        _storeReader.FindAsync("daily-report", Arg.Any<CancellationToken>())
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
        _storeReader.FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BackgroundJobDefinition?>(null));

        BackgroundJobManager sut = MakeSut();

        // Act
        Func<Task> act = () =>
            sut.TriggerNowAsync("ghost", TestContext.Current.CancellationToken);

        // Assert
        await Should.ThrowAsync<EntityNotFoundException>(act);
    }
}
