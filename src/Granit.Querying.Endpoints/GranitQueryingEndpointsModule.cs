using Granit.Authorization;
using Granit.Core.Modularity;

namespace Granit.Querying.Endpoints;

/// <summary>
/// Granit module for querying HTTP endpoints.
/// </summary>
/// <remarks>
/// Exposes query list routes via
/// <c>MapQueryEndpoints&lt;TEntity, TDto&gt;()</c>,
/// the <c>GET /meta</c> metadata endpoint, and CRUD saved view endpoints.
/// Requires both <see cref="GranitQueryingModule"/> (core infrastructure)
/// and <see cref="GranitAuthorizationModule"/> (permission policy enforcement).
/// </remarks>
[DependsOn(
    typeof(GranitQueryingModule),
    typeof(GranitAuthorizationModule))]
public sealed class GranitQueryingEndpointsModule : GranitModule;
