using Granit.Authorization.Abstractions;

namespace Granit.Timeline.Endpoints.Permissions;

/// <summary>
/// Declares the <c>Timeline.Entries.Read</c> and <c>Timeline.Entries.Create</c> permissions
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
            TimelinePermissions.Entries.Read,
            "Consulter les flux d'activité et l'historique d'audit");

        group.AddPermission(
            TimelinePermissions.Entries.Create,
            "Poster des commentaires et suivre/ne plus suivre des entités");
    }
}
