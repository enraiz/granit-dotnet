using Granit.Authorization.Abstractions;

namespace Granit.Timeline.Endpoints.Permissions;

/// <summary>
/// Declares the <c>Timeline.Read</c> and <c>Timeline.Write</c> permissions
/// in the Granit RBAC system.
/// </summary>
/// <remarks>
/// Registered automatically by <see cref="GranitTimelineEndpointsModule"/>.
/// Once registered, <c>DynamicPermissionPolicyProvider</c> creates the authorization policies
/// via <c>PermissionRequirement</c> — the full <c>IPermissionChecker</c> pipeline is used.
/// </remarks>
internal sealed class TimelinePermissionDefinitionProvider : IPermissionDefinitionProvider
{
    /// <inheritdoc />
    public void DefinePermissions(IPermissionDefinitionContext context)
    {
        PermissionGroup group = context.AddGroup(
            TimelinePermissions.GroupName, "Timeline");

        group.AddPermission(
            TimelinePermissions.Read.Default,
            "Consulter les flux d'activité et l'historique d'audit");

        group.AddPermission(
            TimelinePermissions.Write.Default,
            "Poster des commentaires, gérer les entrées et suivre/ne plus suivre des entités");
    }
}
