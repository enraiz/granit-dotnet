using Granit.Workflow.Endpoints.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Granit.Workflow.Endpoints.Extensions;

/// <summary>
/// Extension methods for registering workflow endpoints.
/// </summary>
public static class WorkflowEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the workflow endpoints (transition history, status) onto the given route builder.
    /// </summary>
    /// <remarks>
    /// <para>Registers the following routes:</para>
    /// <list type="bullet">
    ///   <item><c>GET /{entityType}/{entityId}/history</c> — HDS audit trail</item>
    /// </list>
    /// <para>Call this from your application route registration:</para>
    /// <code>
    /// app.MapWorkflowEndpoints();
    ///
    /// // With a custom prefix:
    /// app.MapWorkflowEndpoints(opts =&gt; opts.RoutePrefix = "admin/workflow");
    /// </code>
    /// </remarks>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="configure">Optional delegate to customize options.</param>
    /// <returns>The <see cref="RouteGroupBuilder"/> for further chaining.</returns>
    public static RouteGroupBuilder MapWorkflowEndpoints(
        this IEndpointRouteBuilder endpoints,
        Action<WorkflowEndpointsOptions>? configure = null)
    {
        WorkflowEndpointsOptions options = new();
        configure?.Invoke(options);

        RouteGroupBuilder group = endpoints
            .MapGroup(options.RoutePrefix)
            .WithTags(options.TagName);

        group.MapReadEndpoints();

        return group;
    }
}
