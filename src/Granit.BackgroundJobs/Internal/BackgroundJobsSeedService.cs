using Microsoft.Extensions.Hosting;

namespace Granit.BackgroundJobs.Internal;

/// <summary>
/// Hosted service that seeds the <see cref="IBackgroundJobStore"/> with all jobs
/// discovered at startup via <see cref="RecurringJobDiscovery"/>.
/// Runs once on application start, before any Wolverine handlers are invoked.
/// </summary>
internal sealed class BackgroundJobsSeedService(
    IBackgroundJobStore store,
    IReadOnlyList<RecurringJobRegistration> registrations) : IHostedService
{
    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken) =>
        store.SeedJobsAsync(registrations, cancellationToken);

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
