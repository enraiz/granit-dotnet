using Granit.BackgroundJobs.Internal;
using Granit.Guids;
using Granit.Security;
using Granit.Timing;
using JasperFx.Core;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.Core;
using Shouldly;
using Wolverine;
using Wolverine.Persistence.Durability;
using Wolverine.Persistence.Durability.DeadLetterManagement;
using Xunit;

namespace Granit.BackgroundJobs.Tests.Integration;

// Duplicated from Granit.BackgroundJobs.Tests to avoid cross-test-project references.
[RecurringJob("0 8 * * *", "fake-daily-report")]
public sealed class FakeDailyReportMessage;

[RecurringJob("0 * * * *", "fake-hourly-cleanup")]
public sealed class FakeHourlyCleanupMessage;

/// <summary>
/// Integration tests validating interactions between real components
/// (InMemoryBackgroundJobStore + RecurringJobSchedulingMiddleware + BackgroundJobManager)
/// without a full Wolverine runtime.
/// </summary>
public sealed class BackgroundJobsIntegrationTests
{
    // FakeDailyReportMessage and FakeHourlyCleanupMessage are defined above in this file.

    private static RecurringJobRegistration DailyRegistration() =>
        new("fake-daily-report", "0 8 * * *",
            typeof(FakeDailyReportMessage).AssemblyQualifiedName!);

    // =========================================================================
    // AfterAsync end-to-end: real store + middleware
    // =========================================================================

    [Fact]
    public async Task AfterAsync_EndToEnd_SchedulesNextOccurrenceInStore()
    {
        // Arrange
        InMemoryBackgroundJobStore store = new(new SimpleGuidGenerator());
        await store.SeedJobsAsync([DailyRegistration()], TestContext.Current.CancellationToken);

        // Clock at 07:00 UTC — next "0 8 * * *" occurrence is at 08:00 same day
        DateTimeOffset now = new(2026, 3, 1, 7, 0, 0, TimeSpan.Zero);
        IClock clock = Substitute.For<IClock>();
        clock.Now.Returns(now);

        IMessageContext context = Substitute.For<IMessageContext>();
        RecurringJobSchedulingMiddleware middleware = new(
            store, store, clock, NullLogger<RecurringJobSchedulingMiddleware>.Instance);
        Envelope envelope = new(new FakeDailyReportMessage());

        // Act
        await middleware.AfterAsync(envelope, context, TestContext.Current.CancellationToken);

        // Assert — store updated with computed next occurrence
        BackgroundJobDefinition? job = await store.FindAsync(
            "fake-daily-report", TestContext.Current.CancellationToken);
        DateTimeOffset expectedNext = new(2026, 3, 1, 8, 0, 0, TimeSpan.Zero);
        job!.NextExecutionAt.ShouldBe(expectedNext);
    }

    // =========================================================================
    // Pause blocks rescheduling
    // =========================================================================

    [Fact]
    public async Task AfterAsync_AfterPause_DoesNotUpdateNextExecutionAt()
    {
        // Arrange
        InMemoryBackgroundJobStore store = new(new SimpleGuidGenerator());
        await store.SeedJobsAsync([DailyRegistration()], TestContext.Current.CancellationToken);
        await store.SetEnabledAsync(
            "fake-daily-report", false, TestContext.Current.CancellationToken);

        IClock clock = Substitute.For<IClock>();
        clock.Now.Returns(new DateTimeOffset(2026, 3, 1, 7, 0, 0, TimeSpan.Zero));

        IMessageContext context = Substitute.For<IMessageContext>();
        RecurringJobSchedulingMiddleware middleware = new(
            store, store, clock, NullLogger<RecurringJobSchedulingMiddleware>.Instance);
        Envelope envelope = new(new FakeDailyReportMessage());

        // Act
        await middleware.AfterAsync(envelope, context, TestContext.Current.CancellationToken);

        // Assert — NextExecutionAt not updated (still null after seed)
        BackgroundJobDefinition? job = await store.FindAsync(
            "fake-daily-report", TestContext.Current.CancellationToken);
        job!.NextExecutionAt.ShouldBeNull();
    }

    // =========================================================================
    // TriggerNow + TriggeredBy persisted (end-to-end header propagation)
    // =========================================================================

    [Fact]
    public async Task TriggerNow_TriggeredByHeader_PersistedAfterBeforeAsync()
    {
        // Arrange
        InMemoryBackgroundJobStore store = new(new SimpleGuidGenerator());
        await store.SeedJobsAsync([DailyRegistration()], TestContext.Current.CancellationToken);

        IClock clock = Substitute.For<IClock>();
        clock.Now.Returns(DateTimeOffset.UtcNow);

        ICurrentUserService user = Substitute.For<ICurrentUserService>();
        user.IsAuthenticated.Returns(true);
        user.UserId.Returns("user-admin");

        IMessageBus bus = Substitute.For<IMessageBus>();
        IMessageStore messageStore = Substitute.For<IMessageStore>();
        IDeadLetters deadLetters = Substitute.For<IDeadLetters>();
        messageStore.DeadLetters.Returns(deadLetters);
        deadLetters
            .SummarizeAllAsync(Arg.Any<string>(), Arg.Any<TimeRange>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<DeadLetterQueueCount>>([]));

        BackgroundJobManager manager = new(
            store, store, bus, clock, user, NullLogger<BackgroundJobManager>.Instance, messageStore);
        RecurringJobSchedulingMiddleware middleware = new(
            store, store, clock, NullLogger<RecurringJobSchedulingMiddleware>.Instance);

        // Act — TriggerNow injects X-Triggered-By into DeliveryOptions
        await manager.TriggerNowAsync("fake-daily-report", TestContext.Current.CancellationToken);

        // Recover published call args via NSubstitute ReceivedCalls()
        ICall publishCall = bus.ReceivedCalls()
            .First(c => c.GetMethodInfo().Name == nameof(IMessageBus.PublishAsync));
        object capturedMessage = publishCall.GetArguments()[0]!;
        var capturedOptions = (DeliveryOptions)publishCall.GetArguments()[1]!;

        // Simulate Wolverine copying DeliveryOptions.Headers into Envelope.Headers
        Envelope envelope = new(capturedMessage);
        foreach (KeyValuePair<string, string?> kv in capturedOptions.Headers)
        {
            if (kv.Value is not null)
            {
                envelope.Headers[kv.Key] = kv.Value;
            }
        }

        await middleware.BeforeAsync(envelope, TestContext.Current.CancellationToken);

        // Assert — TriggeredBy persisted in store
        BackgroundJobDefinition? job = await store.FindAsync(
            "fake-daily-report", TestContext.Current.CancellationToken);
        job!.TriggeredBy.ShouldBe("user-admin");
    }

    // =========================================================================
    // Seeding idempotent
    // =========================================================================

    [Fact]
    public async Task SeedJobsAsync_CalledTwice_DoesNotDuplicateJobs()
    {
        // Arrange
        InMemoryBackgroundJobStore store = new(new SimpleGuidGenerator());
        RecurringJobRegistration[] registrations =
        [
            new("job-a", "0 * * * *", typeof(FakeDailyReportMessage).AssemblyQualifiedName!),
            new("job-b", "0 8 * * *", typeof(FakeHourlyCleanupMessage).AssemblyQualifiedName!),
        ];

        // Act — seed twice simulating two startup calls
        await store.SeedJobsAsync(registrations, TestContext.Current.CancellationToken);
        await store.SeedJobsAsync(registrations, TestContext.Current.CancellationToken);

        // Assert — exactly 2 jobs, no duplicates
        IReadOnlyList<BackgroundJobDefinition> jobs =
            await store.GetAllJobsAsync(TestContext.Current.CancellationToken);
        jobs.Count.ShouldBe(2);
        jobs.Select(j => j.JobName).Order().ShouldBe(["job-a", "job-b"]);
    }
}
