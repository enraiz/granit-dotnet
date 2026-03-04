using Granit.BackgroundJobs.Endpoints.Permissions;

namespace Granit.BackgroundJobs.Endpoints.Internal;

/// <summary>
/// Authorization policy constants for background jobs endpoints.
/// </summary>
public static class BackgroundJobsAuthorizationPolicy
{
    /// <summary>
    /// Name of the authorization policy that guards all background jobs administration endpoints.
    /// Equals <see cref="BackgroundJobsPermissions.Jobs.Manage"/> (<c>"BackgroundJobs.Jobs.Manage"</c>).
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
    /// <see cref="Extensions.BackgroundJobsEndpointRouteBuilderExtensions.MapBackgroundJobsEndpoints"/>.
    /// </para>
    /// </remarks>
    public const string PolicyName = BackgroundJobsPermissions.Jobs.Manage;
}
