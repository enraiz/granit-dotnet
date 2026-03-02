using Granit.DataImport.Endpoints.Endpoints;
using Granit.DataImport.Endpoints.Internal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Granit.DataImport.Endpoints.Extensions;

/// <summary>
/// Extension methods for registering data import endpoints.
/// </summary>
public static class DataImportEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the data import endpoints onto the given route builder.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Registers the <c>DataImport.Admin</c> authorization policy (see
    /// <see cref="DataImportAuthorizationPolicy.PolicyName"/>) requiring the role
    /// configured via <see cref="DataImportEndpointsOptions.RequiredRole"/>.
    /// </para>
    /// <para>Call this from your application route registration:</para>
    /// <code>
    /// app.MapDataImportEndpoints();
    ///
    /// // With a custom prefix or role:
    /// app.MapDataImportEndpoints(opts =>
    /// {
    ///     opts.RoutePrefix = "admin/imports";
    ///     opts.RequiredRole = "ops-team";
    /// });
    /// </code>
    /// <para>
    /// Exposes 9 endpoints: POST /, POST /{jobId}/preview, PUT /{jobId}/mappings,
    /// POST /{jobId}/execute, POST /{jobId}/dry-run, GET /{jobId},
    /// DELETE /{jobId}, GET /{jobId}/report, GET /{jobId}/correction-file.
    /// </para>
    /// </remarks>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="configure">Optional delegate to customize <see cref="DataImportEndpointsOptions"/>.</param>
    /// <returns>The <see cref="RouteGroupBuilder"/> for further chaining.</returns>
    public static RouteGroupBuilder MapDataImportEndpoints(
        this IEndpointRouteBuilder endpoints,
        Action<DataImportEndpointsOptions>? configure = null)
    {
        DataImportEndpointsOptions options = new();
        configure?.Invoke(options);

        string prefix = string.IsNullOrEmpty(options.ApiPrefix)
            ? options.RoutePrefix
            : $"{options.ApiPrefix.TrimEnd('/')}/{options.RoutePrefix.TrimStart('/')}";

        // Register the named authorization policy so that endpoints can use
        // RequireAuthorization(PolicyName). This is safe to call here because
        // IOptions<AuthorizationOptions> is a singleton and is evaluated lazily
        // (before the first policy lookup at request time).
        IOptions<AuthorizationOptions>? authOptions =
            endpoints.ServiceProvider.GetService<IOptions<AuthorizationOptions>>();
        authOptions?.Value.AddPolicy(
            DataImportAuthorizationPolicy.PolicyName,
            policy => policy.RequireRole(options.RequiredRole));

        RouteGroupBuilder group = endpoints
            .MapGroup(prefix)
            .WithTags(options.TagName)
            .RequireAuthorization(DataImportAuthorizationPolicy.PolicyName);

        group.MapUploadEndpoints();
        group.MapExecutionEndpoints();
        group.MapReportEndpoints();

        return group;
    }
}
