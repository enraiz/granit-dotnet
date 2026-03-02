using Granit.DataImport.Endpoints.Permissions;

namespace Granit.DataImport.Endpoints.Internal;

/// <summary>
/// Authorization policy constants for data import endpoints.
/// </summary>
public static class DataImportAuthorizationPolicy
{
    /// <summary>
    /// Name of the authorization policy that guards all data import endpoints.
    /// Equals <see cref="DataImportPermissions.Admin.Default"/> (<c>"DataImport.Admin"</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>GranitAuthorizationModule</c> is loaded, <c>DynamicPermissionPolicyProvider</c>
    /// resolves this policy via <c>PermissionRequirement</c> — the full <c>IPermissionChecker</c>
    /// pipeline applies (AdminRole bypass, cache, <c>IPermissionGrantStore</c>).
    /// </para>
    /// <para>
    /// Without <c>GranitAuthorizationModule</c> (e.g. unit tests), the policy falls back to
    /// the role-based check registered by
    /// <see cref="Extensions.DataImportEndpointRouteBuilderExtensions.MapDataImportEndpoints"/>.
    /// </para>
    /// </remarks>
    public const string PolicyName = DataImportPermissions.Admin.Default;
}
