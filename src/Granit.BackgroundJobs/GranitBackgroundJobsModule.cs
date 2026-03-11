using Granit.BackgroundJobs.Domain;
using Granit.BackgroundJobs.Extensions;
using Granit.BackgroundJobs.Options;
using Granit.Core.Modularity;
using Granit.Wolverine;

namespace Granit.BackgroundJobs;

/// <summary>
/// Granit module for recurring background jobs (provider-agnostic core).
/// </summary>
/// <remarks>
/// Registers the background jobs infrastructure on top of <see cref="GranitWolverineModule"/>.
/// <para>
/// Mode selection is driven by <see cref="BackgroundJobsOptions.Mode"/>
/// (bound from the <c>"BackgroundJobs"</c> configuration section):
/// <list type="bullet">
///   <item><see cref="JobStoreMode.InMemory"/> — no DB required; suitable for development and tests.</item>
///   <item><see cref="JobStoreMode.Durable"/> — EF Core store; requires <see cref="BackgroundJobsOptions.ConnectionString"/>.</item>
/// </list>
/// </para>
/// </remarks>
[DependsOn(typeof(GranitWolverineModule))]
public sealed class GranitBackgroundJobsModule : GranitModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Builder.AddGranitBackgroundJobs();
}
