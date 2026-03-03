using Granit.DataExchange.Endpoints.Endpoints.Export;
using Granit.DataExchange.Endpoints.Endpoints.Import;
using Granit.DataExchange.Endpoints.Internal.Export;
using Granit.DataExchange.Endpoints.Internal.Import;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Granit.DataExchange.Endpoints.Extensions;

/// <summary>
/// Extension methods for registering data import endpoints.
/// </summary>
public static class DataExchangeEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the data import endpoints onto the given route builder.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Registers the <c>DataExchange.Import</c> authorization policy (see
    /// <see cref="ImportAuthorizationPolicy.PolicyName"/>) requiring the role
    /// configured via <see cref="DataExchangeEndpointsOptions.RequiredRole"/>.
    /// </para>
    /// <para>Call this from your application route registration:</para>
    /// <code>
    /// app.MapDataExchangeEndpoints();
    ///
    /// // With a custom prefix or role:
    /// app.MapDataExchangeEndpoints(opts =>
    /// {
    ///     opts.RoutePrefix = "admin/imports";
    ///     opts.RequiredRole = "ops-team";
    /// });
    /// </code>
    /// <para>
    /// Exposes 9 import endpoints: POST /, POST /{jobId}/preview, PUT /{jobId}/mappings,
    /// POST /{jobId}/execute, POST /{jobId}/dry-run, GET /{jobId},
    /// DELETE /{jobId}, GET /{jobId}/report, GET /{jobId}/correction-file.
    /// </para>
    /// <para>
    /// Also exposes 6 export endpoints under <c>/export/</c>:
    /// GET /export/definitions, GET /export/definitions/{name}/fields,
    /// POST /export/jobs, GET /export/jobs/{id}, GET /export/jobs/{id}/download,
    /// GET /export/presets/{definitionName}, POST /export/presets,
    /// DELETE /export/presets/{definitionName}/{presetName}.
    /// </para>
    /// </remarks>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="configure">Optional delegate to customize <see cref="DataExchangeEndpointsOptions"/>.</param>
    /// <returns>The <see cref="RouteGroupBuilder"/> for further chaining.</returns>
    public static RouteGroupBuilder MapDataExchangeEndpoints(
        this IEndpointRouteBuilder endpoints,
        Action<DataExchangeEndpointsOptions>? configure = null)
    {
        DataExchangeEndpointsOptions options = new();
        configure?.Invoke(options);

        string prefix = string.IsNullOrEmpty(options.ApiPrefix)
            ? options.RoutePrefix
            : $"{options.ApiPrefix.TrimEnd('/')}/{options.RoutePrefix.TrimStart('/')}";

        // Register the named authorization policies so that endpoints can use
        // RequireAuthorization(PolicyName). This is safe to call here because
        // IOptions<AuthorizationOptions> is a singleton and is evaluated lazily
        // (before the first policy lookup at request time).
        IOptions<AuthorizationOptions>? authOptions =
            endpoints.ServiceProvider.GetService<IOptions<AuthorizationOptions>>();
        authOptions?.Value.AddPolicy(
            ImportAuthorizationPolicy.PolicyName,
            policy => policy.RequireRole(options.RequiredRole));
        authOptions?.Value.AddPolicy(
            DataExportAuthorizationPolicy.PolicyName,
            policy => policy.RequireRole(options.RequiredRole));

        RouteGroupBuilder group = endpoints
            .MapGroup(prefix)
            .WithTags(options.TagName)
            .RequireAuthorization(ImportAuthorizationPolicy.PolicyName);

        group.MapUploadEndpoints();
        group.MapExecutionEndpoints();
        group.MapReportEndpoints();

        // Export endpoints under /export/ sub-group with dedicated permission
        RouteGroupBuilder exportGroup = group
            .MapGroup("export")
            .WithTags("Data Export")
            .RequireAuthorization(DataExportAuthorizationPolicy.PolicyName);

        exportGroup.MapExportDefinitionEndpoints();
        exportGroup.MapExportExecutionEndpoints();
        exportGroup.MapExportPresetEndpoints();

        return group;
    }
}
