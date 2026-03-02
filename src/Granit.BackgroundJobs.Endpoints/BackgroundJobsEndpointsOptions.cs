namespace Granit.BackgroundJobs.Endpoints;

/// <summary>
/// Configuration options for the background jobs administration endpoints.
/// Bind from <c>"BackgroundJobsEndpoints"</c> or pass an action to
/// <see cref="Extensions.BackgroundJobsEndpointRouteBuilderExtensions.MapBackgroundJobsEndpoints"/>.
/// </summary>
public sealed class BackgroundJobsEndpointsOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "BackgroundJobsEndpoints";

    /// <summary>
    /// Optional API prefix prepended to <see cref="RoutePrefix"/>
    /// (e.g., <c>"api/v1"</c>). Empty by default (no prefix).
    /// </summary>
    public string ApiPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Route prefix for all background jobs endpoints.
    /// Default: <c>"background-jobs"</c>.
    /// </summary>
    public string RoutePrefix { get; set; } = "background-jobs";

    /// <summary>
    /// Granit permission required to access the administration endpoints.
    /// Default: <c>"granit-background-jobs-admin"</c>.
    /// </summary>
    public string RequiredRole { get; set; } = "granit-background-jobs-admin";

    /// <summary>
    /// OpenAPI tag name for grouping background jobs endpoints in Swagger UI.
    /// Default: <c>"Background Jobs"</c>.
    /// </summary>
    public string TagName { get; set; } = "Background Jobs";
}
