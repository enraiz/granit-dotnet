using Granit.Authorization.Abstractions;

namespace Granit.Identity.Endpoints.Permissions;

/// <summary>
/// Declares identity user cache permissions in the Granit RBAC system.
/// </summary>
internal sealed class IdentityPermissionDefinitionProvider : IPermissionDefinitionProvider
{
    /// <inheritdoc />
    public void DefinePermissions(IPermissionDefinitionContext context)
    {
        PermissionGroup group = context.AddGroup(
            IdentityUserCachePermissions.GroupName, "Identity");

        group.AddPermission(
            IdentityUserCachePermissions.UserCache.Read,
            "Consulter le cache utilisateurs (liste, recherche, batch, stats)");

        group.AddPermission(
            IdentityUserCachePermissions.UserCache.Sync,
            "Forcer la synchronisation du cache depuis le fournisseur d'identité");

        group.AddPermission(
            IdentityUserCachePermissions.UserCache.Delete,
            "Supprimer ou pseudonymiser des entrées du cache (RGPD)");
    }
}
