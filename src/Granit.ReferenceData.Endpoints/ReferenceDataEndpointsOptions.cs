namespace Granit.ReferenceData.Endpoints;

/// <summary>
/// Configuration options for reference data endpoints.
/// </summary>
public sealed class ReferenceDataEndpointsOptions
{
    /// <summary>
    /// Route prefix for all reference data endpoints.
    /// Default: <c>"reference-data"</c>.
    /// </summary>
    public string RoutePrefix { get; set; } = "reference-data";

    /// <summary>
    /// OpenAPI tag name for grouping reference data endpoints in Swagger UI.
    /// Default: <c>"Reference Data"</c>.
    /// </summary>
    public string TagName { get; set; } = "Reference Data";

    /// <summary>
    /// Authorization policy name for admin endpoints (POST, PUT, DELETE).
    /// Set to <c>null</c> to disable authorization. Default: <c>"ReferenceData.Admin"</c>.
    /// </summary>
    public string? AdminPolicyName { get; set; } = "ReferenceData.Admin";

    /// <summary>
    /// Role required for the admin authorization policy (used as fallback
    /// when no dynamic permission system is configured).
    /// Default: <c>"granit-reference-data-admin"</c>.
    /// </summary>
    public string RequiredRole { get; set; } = "granit-reference-data-admin";

}
