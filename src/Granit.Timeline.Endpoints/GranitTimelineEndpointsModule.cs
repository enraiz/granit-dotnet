using Granit.Core.Modularity;

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
[DependsOn(typeof(GranitTimelineModule))]
public sealed class GranitTimelineEndpointsModule : GranitModule;
