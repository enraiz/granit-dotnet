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

    /// <summary>Read permissions for timeline endpoints.</summary>
    public static class Read
    {
        /// <summary>
        /// Grants read access to activity streams, followers, and timeline history.
        /// </summary>
        public const string Default = "Timeline.Read";
    }

    /// <summary>Write permissions for timeline endpoints.</summary>
    public static class Write
    {
        /// <summary>
        /// Grants write access to post comments, manage entries, and follow/unfollow entities.
        /// </summary>
        public const string Default = "Timeline.Write";
    }
}
