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
    /// Optional API prefix prepended to <see cref="RoutePrefix"/>
    /// (e.g., <c>"api/v1"</c>). Empty by default (no prefix).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Example with versioning:
    /// <code>
    /// app.MapGranitLocalization(opts => opts.ApiPrefix = "api/v1");
    /// app.MapGranitLocalizationOverrides(opts => opts.ApiPrefix = "api/v1");
    /// </code>
    /// This produces routes like <c>GET /api/v1/localization</c>.
    /// </para>
    /// </remarks>
    public string ApiPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Route prefix for both the bootstrapping endpoint and the overrides management endpoints.
    /// Default: <c>"localization"</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>MapGranitLocalization</c> registers <c>GET /{EffectivePrefix}</c>.
    /// <c>MapGranitLocalizationOverrides</c> registers CRUD under <c>/{EffectivePrefix}/overrides</c>.
    /// </para>
    /// </remarks>
    public string RoutePrefix { get; set; } = "localization";

    /// <summary>
    /// OpenAPI tag name for all localization endpoints.
    /// Default: <c>"Localization"</c>.
    /// </summary>
    public string TagName { get; set; } = "Localization";
}
