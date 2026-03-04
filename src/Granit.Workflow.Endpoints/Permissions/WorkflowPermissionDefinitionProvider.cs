using Granit.Authorization.Abstractions;

namespace Granit.Workflow.Endpoints.Permissions;

/// <summary>
/// Declares the <c>Workflow.History</c> permission in the Granit RBAC system.
/// </summary>
/// <remarks>
/// Registered automatically by <see cref="GranitWorkflowEndpointsModule"/>.
/// Once registered, <c>DynamicPermissionPolicyProvider</c> creates the authorization policy
/// via <c>PermissionRequirement</c> — the full <c>IPermissionChecker</c> pipeline is used.
/// </remarks>
internal sealed class WorkflowPermissionDefinitionProvider : IPermissionDefinitionProvider
{
    /// <inheritdoc />
    public void DefinePermissions(IPermissionDefinitionContext context)
    {
        PermissionGroup group = context.AddGroup(
            WorkflowPermissions.GroupName, "Workflow");

        group.AddPermission(
            WorkflowPermissions.History.Default,
            "Consulter l'historique des transitions workflow (piste d'audit HDS)");
    }
}
