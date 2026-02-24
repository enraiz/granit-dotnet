using Granit.Authorization;
using Granit.Core.Modularity;

namespace Granit.BackgroundJobs.Endpoints;

/// <summary>
/// Granit module for background jobs administration HTTP endpoints.
/// </summary>
/// <remarks>
/// Exposes job management routes via
/// <see cref="Extensions.BackgroundJobsEndpointRouteBuilderExtensions.MapBackgroundJobsEndpoints"/>.
/// Requires both <see cref="GranitBackgroundJobsModule"/> (job store and manager)
/// and <see cref="GranitAuthorizationModule"/> (permission policy enforcement).
/// </remarks>
[DependsOn(
    typeof(GranitBackgroundJobsModule),
    typeof(GranitAuthorizationModule))]
public sealed class GranitBackgroundJobsEndpointsModule : GranitModule;
