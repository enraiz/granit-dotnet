namespace Granit.Timeline.Endpoints;

/// <summary>
/// Configuration options for the timeline endpoints.
/// </summary>
public sealed class TimelineEndpointsOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "TimelineEndpoints";

    /// <summary>
    /// Optional API prefix prepended to <see cref="RoutePrefix"/>
    /// (e.g., <c>"api/v1"</c>). Empty by default (no prefix).
    /// </summary>
    public string ApiPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Route prefix for all timeline endpoints.
    /// Default: <c>"timeline"</c>.
    /// </summary>
    public string RoutePrefix { get; set; } = "timeline";

    /// <summary>
    /// OpenAPI tag name for grouping timeline endpoints in Swagger UI.
    /// Default: <c>"Timeline"</c>.
    /// </summary>
    public string TagName { get; set; } = "Timeline";
}
