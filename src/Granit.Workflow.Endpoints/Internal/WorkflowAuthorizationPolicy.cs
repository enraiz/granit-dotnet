using Granit.Workflow.Endpoints.Permissions;

namespace Granit.Workflow.Endpoints.Internal;

/// <summary>
/// Authorization policy constants for workflow endpoints.
/// </summary>
public static class WorkflowAuthorizationPolicy
{
    /// <summary>
    /// Name of the authorization policy that guards all workflow administration endpoints.
    /// Equals <see cref="WorkflowPermissions.History.Read"/> (<c>"Workflow.History.Read"</c>).
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
    /// <see cref="Extensions.WorkflowEndpointRouteBuilderExtensions.MapWorkflowEndpoints"/>.
    /// </para>
    /// </remarks>
    public const string PolicyName = WorkflowPermissions.History.Read;
}
