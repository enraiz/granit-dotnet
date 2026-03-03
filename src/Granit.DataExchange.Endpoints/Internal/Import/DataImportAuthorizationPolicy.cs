using Granit.DataExchange.Endpoints.Permissions;

namespace Granit.DataExchange.Endpoints.Internal.Import;

/// <summary>
/// Authorization policy constants for data import endpoints.
/// </summary>
public static class ImportAuthorizationPolicy
{
    /// <summary>
    /// Name of the authorization policy that guards all data import endpoints.
    /// Equals <see cref="DataExchangePermissions.Admin.Default"/> (<c>"DataExchange.Import"</c>).
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
    /// <see cref="Extensions.DataExchangeEndpointRouteBuilderExtensions.MapDataExchangeEndpoints"/>.
    /// </para>
    /// </remarks>
    public const string PolicyName = DataExchangePermissions.Admin.Default;
}
