using Granit.BackgroundJobs.Domain;
using Microsoft.Extensions.Hosting;

namespace Granit.BackgroundJobs.Internal;

/// <summary>
/// Hosted service that seeds the <see cref="IBackgroundJobStoreWriter"/> with all jobs
/// discovered at startup via <see cref="RecurringJobDiscovery"/>.
/// Runs once on application start, before any Wolverine handlers are invoked.
/// </summary>
internal sealed class BackgroundJobsSeedService(
    IBackgroundJobStoreWriter storeWriter,
    IReadOnlyList<RecurringJobRegistration> registrations) : IHostedService
{
    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken) =>
        storeWriter.SeedJobsAsync(registrations, cancellationToken);

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
