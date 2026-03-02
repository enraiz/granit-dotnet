using Granit.Timeline.Endpoints.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Granit.Timeline.Endpoints.Extensions;

/// <summary>
/// Extension methods for registering timeline endpoints.
/// </summary>
public static class TimelineEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the timeline endpoints (stream, entries, followers) onto the given route builder.
    /// </summary>
    /// <remarks>
    /// <para>Registers the following routes:</para>
    /// <list type="bullet">
    ///   <item><c>GET /{entityType}/{entityId}</c> — paginated activity stream</item>
    ///   <item><c>POST /{entityType}/{entityId}/entries</c> — post comment/note</item>
    ///   <item><c>DELETE /entries/{id}</c> — soft-delete by GUID (RGPD)</item>
    ///   <item><c>POST /{entityType}/{entityId}/follow</c> — follow entity</item>
    ///   <item><c>DELETE /{entityType}/{entityId}/follow</c> — unfollow entity</item>
    ///   <item><c>GET /{entityType}/{entityId}/followers</c> — list followers</item>
    /// </list>
    /// <para>Call this from your application route registration:</para>
    /// <code>
    /// app.MapTimelineEndpoints();
    ///
    /// // With a custom prefix:
    /// app.MapTimelineEndpoints(opts =&gt; opts.RoutePrefix = "admin/timeline");
    /// </code>
    /// </remarks>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="configure">Optional delegate to customize options.</param>
    /// <returns>The <see cref="RouteGroupBuilder"/> for further chaining.</returns>
    public static RouteGroupBuilder MapTimelineEndpoints(
        this IEndpointRouteBuilder endpoints,
        Action<TimelineEndpointsOptions>? configure = null)
    {
        TimelineEndpointsOptions options = new();
        configure?.Invoke(options);

        string prefix = string.IsNullOrEmpty(options.ApiPrefix)
            ? options.RoutePrefix
            : $"{options.ApiPrefix.TrimEnd('/')}/{options.RoutePrefix.TrimStart('/')}";

        RouteGroupBuilder group = endpoints
            .MapGroup(prefix)
            .WithTags(options.TagName);

        group.MapStreamEndpoints();
        group.MapEntryEndpoints();
        group.MapFollowerEndpoints();

        return group;
    }
}
