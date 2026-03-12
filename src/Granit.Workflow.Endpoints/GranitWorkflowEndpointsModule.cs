using Granit.Authorization;
using Granit.Core.Modularity;

namespace Granit.Workflow.Endpoints;

/// <summary>
/// Granit module for workflow HTTP endpoints.
/// Exposes workflow transition history and status via Minimal API routes.
/// </summary>
/// <remarks>
/// <para>
/// Map endpoints in your application:
/// <code>
/// app.MapWorkflowEndpoints();
/// </code>
/// </para>
/// <para>
/// Register services:
/// <code>
/// services.AddGranitWorkflowEndpoints();
/// </code>
/// </para>
/// Permission definition providers are auto-discovered by <c>GranitAuthorizationModule</c>.
/// </remarks>
[DependsOn(
    typeof(GranitWorkflowModule),
    typeof(GranitAuthorizationModule))]
public sealed class GranitWorkflowEndpointsModule : GranitModule;
