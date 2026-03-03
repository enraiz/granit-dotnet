namespace Granit.DataExchange.Endpoints;

/// <summary>
/// Configuration options for the data import endpoints.
/// Bind from <c>"DataExchangeEndpoints"</c> or pass an action to
/// <see cref="Extensions.DataExchangeEndpointRouteBuilderExtensions.MapDataExchangeEndpoints"/>.
/// </summary>
public sealed class DataExchangeEndpointsOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "DataExchangeEndpoints";

    /// <summary>
    /// Optional API prefix prepended to <see cref="RoutePrefix"/>
    /// (e.g., <c>"api/v1"</c>). Empty by default (no prefix).
    /// </summary>
    public string ApiPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Route prefix for all data import endpoints.
    /// Default: <c>"data-import"</c>.
    /// </summary>
    public string RoutePrefix { get; set; } = "data-import";

    /// <summary>
    /// Granit permission required to access the import endpoints.
    /// Default: <c>"granit-data-import-admin"</c>.
    /// </summary>
    public string RequiredRole { get; set; } = "granit-data-import-admin";

    /// <summary>
    /// OpenAPI tag name for grouping data import endpoints in Swagger UI.
    /// Default: <c>"Data Exchange"</c>.
    /// </summary>
    public string TagName { get; set; } = "Data Exchange";
}
