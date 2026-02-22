// =============================================================================
// MultiTenancyOptions - MultiTenancy module configuration
// =============================================================================

namespace DigitalDynamics.Foundation.MultiTenancy;

/// <summary>
/// Configuration options for the MultiTenancy module.
/// </summary>
public sealed class MultiTenancyOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "MultiTenancy";

    /// <summary>
    /// Enables or disables tenant resolution by the middleware.
    /// Disable in single-tenant environments or for tests.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// JWT claim type containing the tenant identifier.
    /// Default value: "tenant_id" (standard Keycloak claim).
    /// </summary>
    public string TenantIdClaimType { get; set; } = "tenant_id";

    /// <summary>
    /// HTTP header name containing the tenant identifier.
    /// Default value: "X-Tenant-Id".
    /// </summary>
    public string TenantIdHeaderName { get; set; } = "X-Tenant-Id";
}
