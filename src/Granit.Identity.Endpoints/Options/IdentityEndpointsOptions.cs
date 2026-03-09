namespace Granit.Identity.Endpoints.Options;

/// <summary>
/// Configuration options for identity user cache endpoints.
/// </summary>
public sealed class IdentityEndpointsOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "IdentityEndpoints";

    /// <summary>
    /// Route prefix for all identity user cache endpoints.
    /// Default: <c>"identity/users"</c>.
    /// </summary>
    public string RoutePrefix { get; set; } = "identity/users";

    /// <summary>
    /// OpenAPI tag name for grouping identity endpoints in Swagger UI.
    /// Default: <c>"Identity User Cache"</c>.
    /// </summary>
    public string TagName { get; set; } = "Identity User Cache";

    /// <summary>
    /// Role required for fallback authorization policies (when dynamic permission system
    /// is not configured). Default: <c>"granit-identity-admin"</c>.
    /// </summary>
    public string RequiredRole { get; set; } = "granit-identity-admin";
}
