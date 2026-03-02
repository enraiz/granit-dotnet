namespace Granit.Localization.Endpoints;

/// <summary>
/// Configuration options for the Granit localization HTTP endpoints.
/// Pass an action to
/// <see cref="Extensions.LocalizationEndpointRouteBuilderExtensions.MapGranitLocalization"/> or
/// <see cref="Extensions.LocalizationEndpointRouteBuilderExtensions.MapGranitLocalizationOverrides"/>
/// to customize the route prefix.
/// </summary>
public sealed class LocalizationEndpointsOptions
{
    /// <summary>
    /// Route prefix for both the bootstrapping endpoint and the overrides management endpoints.
    /// Default: <c>"api/granit/localization"</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>MapGranitLocalization</c> registers <c>GET /{RoutePrefix}</c>.
    /// <c>MapGranitLocalizationOverrides</c> registers CRUD under <c>/{RoutePrefix}/overrides</c>.
    /// </para>
    /// <para>
    /// Change this to include a version segment when needed:
    /// <code>
    /// app.MapGranitLocalization(opts => opts.RoutePrefix = "api/v1/granit/localization");
    /// app.MapGranitLocalizationOverrides(opts => opts.RoutePrefix = "api/v1/granit/localization");
    /// </code>
    /// </para>
    /// </remarks>
    public string RoutePrefix { get; set; } = "api/granit/localization";

    /// <summary>
    /// OpenAPI tag name for all localization endpoints.
    /// Default: <c>"Localization"</c>.
    /// </summary>
    public string TagName { get; set; } = "Localization";
}
