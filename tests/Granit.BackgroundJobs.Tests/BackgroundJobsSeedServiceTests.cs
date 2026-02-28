using Granit.BackgroundJobs.Internal;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.BackgroundJobs.Tests;

public sealed class BackgroundJobsSeedServiceTests
{
    [Fact]
    public async Task StartAsync_CallsSeedJobsAsync()
    {
        // Arrange
        IBackgroundJobStore store = Substitute.For<IBackgroundJobStore>();
        IReadOnlyList<RecurringJobRegistration> registrations = [
            new RecurringJobRegistration("job-a", "0 * * * *", "MyMessage, MyAssembly")
        ];
        BackgroundJobsSeedService sut = new(store, registrations);
        CancellationToken ct = TestContext.Current.CancellationToken;

        // Act
        await sut.StartAsync(ct);

        // Assert
        await store.Received(1).SeedJobsAsync(registrations, ct);
    }

    [Fact]
    public async Task StopAsync_CompletesWithoutSideEffects()
    {
        // Arrange
        IBackgroundJobStore store = Substitute.For<IBackgroundJobStore>();
        BackgroundJobsSeedService sut = new(store, []);

        // Act
        Func<Task> act = () => sut.StopAsync(TestContext.Current.CancellationToken);

        // Assert
        await Should.NotThrowAsync(act);
        await store.DidNotReceive().SeedJobsAsync(Arg.Any<IEnumerable<RecurringJobRegistration>>(),
            Arg.Any<CancellationToken>());
    }
}
