using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Granit.BackgroundJobs.Endpoints.Extensions;

/// <summary>
/// Extension methods for registering background jobs administration endpoints.
/// </summary>
public static class BackgroundJobsEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the background jobs administration endpoint group onto the given route builder.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Call this from your application's route registration:
    /// </para>
    /// <code>
    /// app.MapBackgroundJobsEndpoints();
    ///
    /// // Or with a custom prefix:
    /// app.MapBackgroundJobsEndpoints(opts => opts.RoutePrefix = "admin/jobs");
    /// </code>
    /// <para>
    /// All routes are protected by the <see cref="BackgroundJobsEndpointsOptions.RequiredRole"/> permission.
    /// Actual endpoint handlers (GET, POST pause/resume/trigger) are registered by this method.
    /// </para>
    /// </remarks>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="configure">Optional delegate to customize <see cref="BackgroundJobsEndpointsOptions"/>.</param>
    /// <returns>The <see cref="RouteGroupBuilder"/> for further chaining.</returns>
    public static RouteGroupBuilder MapBackgroundJobsEndpoints(
        this IEndpointRouteBuilder endpoints,
        Action<BackgroundJobsEndpointsOptions>? configure = null)
    {
        BackgroundJobsEndpointsOptions options = new();
        configure?.Invoke(options);

        RouteGroupBuilder group = endpoints
            .MapGroup(options.RoutePrefix)
            .WithTags(options.TagName)
            .RequireAuthorization(options.RequiredRole);

        return group;
    }
}
