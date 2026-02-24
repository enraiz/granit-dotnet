using Granit.Authorization;
using Granit.Authorization.Abstractions;
using Granit.BackgroundJobs.Endpoints.Permissions;
using Granit.Core.Modularity;
using Microsoft.Extensions.DependencyInjection;

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
public sealed class GranitBackgroundJobsEndpointsModule : GranitModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddSingleton<IPermissionDefinitionProvider,
            BackgroundJobsPermissionDefinitionProvider>();
}
