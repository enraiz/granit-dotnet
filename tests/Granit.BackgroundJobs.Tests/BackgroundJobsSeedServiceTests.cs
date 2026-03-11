using Granit.BackgroundJobs.Domain;
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
        IBackgroundJobStoreWriter storeWriter = Substitute.For<IBackgroundJobStoreWriter>();
        IReadOnlyList<RecurringJobRegistration> registrations = [
            new RecurringJobRegistration("job-a", "0 * * * *", "MyMessage, MyAssembly")
        ];
        BackgroundJobsSeedService sut = new(storeWriter, registrations);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        // Act
        await sut.StartAsync(cancellationToken);

        // Assert
        await storeWriter.Received(1).SeedJobsAsync(registrations, cancellationToken);
    }

    [Fact]
    public async Task StopAsync_CompletesWithoutSideEffects()
    {
        // Arrange
        IBackgroundJobStoreWriter storeWriter = Substitute.For<IBackgroundJobStoreWriter>();
        BackgroundJobsSeedService sut = new(storeWriter, []);

        // Act
        Func<Task> act = () => sut.StopAsync(TestContext.Current.CancellationToken);

        // Assert
        await Should.NotThrowAsync(act);
        await storeWriter.DidNotReceive().SeedJobsAsync(Arg.Any<IEnumerable<RecurringJobRegistration>>(),
            Arg.Any<CancellationToken>());
    }
}
