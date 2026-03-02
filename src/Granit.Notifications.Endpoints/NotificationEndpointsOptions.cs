namespace Granit.Notifications.Endpoints;

/// <summary>
/// Configuration options for the Granit.Notifications HTTP endpoints.
/// </summary>
public sealed class NotificationEndpointsOptions
{
    /// <summary>
    /// Optional API prefix prepended to <see cref="RoutePrefix"/>
    /// (e.g., <c>"api/v1"</c>). Empty by default (no prefix).
    /// </summary>
    public string ApiPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Route prefix for all notification endpoints.
    /// Default: <c>"notifications"</c>.
    /// </summary>
    public string RoutePrefix { get; set; } = "notifications";

    /// <summary>
    /// OpenAPI tag name for all notification endpoints.
    /// Default: <c>"Notifications"</c>.
    /// </summary>
    public string TagName { get; set; } = "Notifications";
}
