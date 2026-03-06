using System.Diagnostics.CodeAnalysis;

namespace Granit.Identity.Endpoints.Permissions;

/// <summary>
/// Permission constants for the <c>Granit.Identity.Endpoints</c> module.
/// </summary>
[SuppressMessage("Naming", "CA1716:Identifiers should not match keywords",
    Justification = "Permission resource names follow [Module].[Resource].[Action] convention")]
public static class IdentityUserCachePermissions
{
    /// <summary>Permission group name.</summary>
    public const string GroupName = "Identity";

    /// <summary>Permissions for reading the user cache.</summary>
    public static class UserCache
    {
        /// <summary>Grants access to list, search, batch resolve, and view stats.</summary>
        public const string Read = "Identity.UserCache.Read";

        /// <summary>Grants access to force sync (single or full) from the identity provider.</summary>
        public const string Sync = "Identity.UserCache.Sync";

        /// <summary>Grants access to RGPD erasure and pseudonymization.</summary>
        public const string Delete = "Identity.UserCache.Delete";
    }
}
