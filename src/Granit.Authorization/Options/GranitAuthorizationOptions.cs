namespace Granit.Authorization.Options;

/// <summary>Configuration options for the Granit.Authorization module.</summary>
public sealed class GranitAuthorizationOptions
{
    /// <summary>Configuration section name in appsettings.json.</summary>
    public const string SectionName = "Authorization";

    /// <summary>
    /// Roles that bypass all permission checks. These roles are the root of trust
    /// and cannot be restricted via <c>IPermissionManager.SetAsync()</c>.
    /// Defaults to <c>["admin"]</c>.
    /// </summary>
    public IList<string> AdminRoles { get; set; } = ["admin"];

    /// <summary>
    /// Duration for which permission check results are cached per (TenantId, RoleName, PermissionName).
    /// Defaults to 5 minutes. Should be less than or equal to the JWT token lifetime.
    /// </summary>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// When true, all permission checks return granted without hitting the cache or store.
    /// For development and testing only — never enable in production.
    /// </summary>
    public bool AlwaysAllow { get; set; }
}
