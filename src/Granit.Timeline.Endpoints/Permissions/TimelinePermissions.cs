namespace Granit.Timeline.Endpoints.Permissions;

/// <summary>
/// Permission constants for the <c>Granit.Timeline.Endpoints</c> module.
/// Use these names when granting permissions via <c>IPermissionManager.SetAsync()</c>
/// or when checking access via <c>IPermissionChecker.IsGrantedAsync()</c>.
/// </summary>
public static class TimelinePermissions
{
    /// <summary>Permission group name used in <c>IPermissionDefinitionContext.AddGroup()</c>.</summary>
    public const string GroupName = "Timeline";

    /// <summary>Permissions for the timeline entries resource.</summary>
    public static class Entries
    {
        /// <summary>
        /// Grants read access to activity streams, followers, and timeline history.
        /// </summary>
        public const string Read = "Timeline.Entries.Read";

        /// <summary>
        /// Grants access to create timeline entries (post comments, follow/unfollow entities).
        /// </summary>
        public const string Create = "Timeline.Entries.Create";
    }
}
