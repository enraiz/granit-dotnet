using Granit.Core.Modularity;
using Granit.Workflow.EntityFrameworkCore;

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
/// services.AddGranitWorkflowEndpoints&lt;AppDbContext&gt;();
/// </code>
/// </para>
/// </remarks>
[DependsOn(
    typeof(GranitWorkflowModule),
    typeof(GranitWorkflowEntityFrameworkCoreModule))]
public sealed class GranitWorkflowEndpointsModule : GranitModule;
