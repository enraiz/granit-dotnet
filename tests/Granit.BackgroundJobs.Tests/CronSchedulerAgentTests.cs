using FluentAssertions;
using Granit.BackgroundJobs.Internal;
using Granit.Timing;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Wolverine;
using Xunit;

namespace Granit.BackgroundJobs.Tests;

public sealed class CronSchedulerAgentTests
{
    // =========================================================================
    // Test infrastructure
    // =========================================================================

    private readonly IBackgroundJobStore _store = Substitute.For<IBackgroundJobStore>();
    private readonly IMessageBus _bus = Substitute.For<IMessageBus>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public CronSchedulerAgentTests()
    {
        _clock.Now.Returns(new DateTimeOffset(2026, 2, 20, 8, 0, 0, TimeSpan.Zero));
    }

    private CronSchedulerAgent CreateAgent() =>
        new(_store, _bus, _clock, NullLogger<CronSchedulerAgent>.Instance);

    private static BackgroundJobDefinition MakeJob(
        string jobName,
        string cron = "0 9 * * *",
        bool isEnabled = true,
        DateTimeOffset? nextExecutionAt = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            JobName = jobName,
            CronExpression = cron,
            MessageType = typeof(FakeJobMessage).AssemblyQualifiedName!,
            IsEnabled = isEnabled,
            NextExecutionAt = nextExecutionAt,
        };

    // Minimal job message class for testing
    private sealed class FakeJobMessage;

    // =========================================================================
    // Scénario: job with null NextExecutionAt → schedules first occurrence
    // =========================================================================

    [Fact]
    public async Task StartAsync_JobWithNoNextExecution_SchedulesFirstOccurrence()
    {
        BackgroundJobDefinition job = MakeJob("daily-sync", cron: "0 9 * * *");
        _store.GetEnabledJobsAsync(Arg.Any<CancellationToken>())
            .Returns([job]);

        await ((Microsoft.Extensions.Hosting.IHostedService)CreateAgent())
            .StartAsync(TestContext.Current.CancellationToken);

        // ScheduleAsync is an extension that delegates to PublishAsync with DeliveryOptions.
        await _bus.Received(1).PublishAsync(
            Arg.Any<FakeJobMessage>(),
            Arg.Any<DeliveryOptions>());
        await _store.Received(1).RecordNextExecutionAsync(
            "daily-sync",
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
    }

    // =========================================================================
    // Scénario: NextExecutionAt is in the future → no duplicate
    // =========================================================================

    [Fact]
    public async Task StartAsync_JobAlreadyScheduledInFuture_DoesNotReschedule()
    {
        DateTimeOffset future = _clock.Now.AddHours(2);
        BackgroundJobDefinition job = MakeJob("daily-sync", nextExecutionAt: future);
        _store.GetEnabledJobsAsync(Arg.Any<CancellationToken>())
            .Returns([job]);

        await ((Microsoft.Extensions.Hosting.IHostedService)CreateAgent())
            .StartAsync(TestContext.Current.CancellationToken);

        await _bus.DidNotReceive().PublishAsync(
            Arg.Any<object>(), Arg.Any<DeliveryOptions>());
        await _store.DidNotReceive().RecordNextExecutionAsync(
            Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    // =========================================================================
    // Scénario: NextExecutionAt is in the past → reschedules
    // =========================================================================

    [Fact]
    public async Task StartAsync_JobWithPastNextExecution_Reschedules()
    {
        DateTimeOffset past = _clock.Now.AddHours(-2);
        BackgroundJobDefinition job = MakeJob("daily-sync", cron: "0 9 * * *", nextExecutionAt: past);
        _store.GetEnabledJobsAsync(Arg.Any<CancellationToken>())
            .Returns([job]);

        await ((Microsoft.Extensions.Hosting.IHostedService)CreateAgent())
            .StartAsync(TestContext.Current.CancellationToken);

        await _bus.Received(1).PublishAsync(
            Arg.Any<FakeJobMessage>(), Arg.Any<DeliveryOptions>());
    }

    // =========================================================================
    // Scénario: paused job → not initialized (filtered by GetEnabledJobsAsync)
    // =========================================================================

    [Fact]
    public async Task StartAsync_NoEnabledJobs_DoesNotScheduleAnything()
    {
        _store.GetEnabledJobsAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<BackgroundJobDefinition>());

        await ((Microsoft.Extensions.Hosting.IHostedService)CreateAgent())
            .StartAsync(TestContext.Current.CancellationToken);

        await _bus.DidNotReceive().PublishAsync(
            Arg.Any<object>(), Arg.Any<DeliveryOptions>());
    }

    // =========================================================================
    // Scénario: idempotence — two startAsync calls don't duplicate
    // =========================================================================

    [Fact]
    public async Task StartAsync_CalledTwice_SchedulesOnlyOnce()
    {
        BackgroundJobDefinition job = MakeJob("daily-sync");
        _store.GetEnabledJobsAsync(Arg.Any<CancellationToken>())
            .Returns([job]);

        Microsoft.Extensions.Hosting.IHostedService agent = CreateAgent();

        await agent.StartAsync(TestContext.Current.CancellationToken);

        // Simulate that RecordNextExecutionAsync updated NextExecutionAt
        job.NextExecutionAt = _clock.Now.AddHours(1);
        _store.GetEnabledJobsAsync(Arg.Any<CancellationToken>())
            .Returns([job]);

        await agent.StartAsync(TestContext.Current.CancellationToken);

        await _bus.Received(1).PublishAsync(
            Arg.Any<FakeJobMessage>(), Arg.Any<DeliveryOptions>());
    }

    // =========================================================================
    // Scénario: invalid cron → skip without scheduling
    // =========================================================================

    [Fact]
    public async Task StartAsync_JobWithInvalidCron_SkipsWithoutScheduling()
    {
        BackgroundJobDefinition job = MakeJob("bad-cron", cron: "NOT_A_CRON");
        _store.GetEnabledJobsAsync(Arg.Any<CancellationToken>()).Returns([job]);

        await ((Microsoft.Extensions.Hosting.IHostedService)CreateAgent())
            .StartAsync(TestContext.Current.CancellationToken);

        await _bus.DidNotReceive().PublishAsync(Arg.Any<object>(), Arg.Any<DeliveryOptions>());
        await _store.DidNotReceive().RecordNextExecutionAsync(
            Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    // =========================================================================
    // Scénario: 6-field cron (with seconds) → schedules using IncludeSeconds parse
    // =========================================================================

    [Fact]
    public async Task StartAsync_JobWithSixFieldCron_SchedulesSuccessfully()
    {
        // "*/30 * * * * *" = every 30 seconds — 6-field cron parsed with IncludeSeconds
        BackgroundJobDefinition job = MakeJob("seconds-job", cron: "*/30 * * * * *");
        _store.GetEnabledJobsAsync(Arg.Any<CancellationToken>()).Returns([job]);

        await ((Microsoft.Extensions.Hosting.IHostedService)CreateAgent())
            .StartAsync(TestContext.Current.CancellationToken);

        await _bus.Received(1).PublishAsync(
            Arg.Any<FakeJobMessage>(), Arg.Any<DeliveryOptions>());
    }

    // =========================================================================
    // CreateMessage — unknown type throws
    // =========================================================================

    [Fact]
    public void CreateMessage_UnknownType_ThrowsInvalidOperationException()
    {
        Action act = () =>
            CronSchedulerAgent.CreateMessage("Unknown.Type, UnknownAssembly", "test-job");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot resolve message type*");
    }

    // =========================================================================
    // StopAsync — no-op
    // =========================================================================

    [Fact]
    public async Task StopAsync_DoesNothing()
    {
        Func<Task> act = () =>
            ((Microsoft.Extensions.Hosting.IHostedService)CreateAgent())
                .StopAsync(TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
    }
}
