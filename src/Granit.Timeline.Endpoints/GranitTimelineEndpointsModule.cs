using Granit.Authorization;
using Granit.Authorization.Abstractions;
using Granit.Core.Modularity;
using Granit.Timeline.Endpoints.Permissions;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.Timeline.Endpoints;

/// <summary>
/// Granit module for timeline HTTP endpoints.
/// Exposes the activity stream, entry management, and follower operations via Minimal API routes.
/// </summary>
/// <remarks>
/// Map endpoints in your application:
/// <code>
/// app.MapTimelineEndpoints();
/// </code>
/// </remarks>
[DependsOn(
    typeof(GranitTimelineModule),
    typeof(GranitAuthorizationModule))]
public sealed class GranitTimelineEndpointsModule : GranitModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddSingleton<IPermissionDefinitionProvider,
            TimelinePermissionDefinitionProvider>();
}
