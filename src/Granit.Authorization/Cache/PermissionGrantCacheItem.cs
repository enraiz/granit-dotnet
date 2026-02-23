namespace Granit.Authorization.Cache;

/// <summary>
/// Cache value for permission grant checks.
/// Public to allow serialization by <c>ICacheService&lt;T&gt;</c> across assembly boundaries.
/// </summary>
public sealed class PermissionGrantCacheItem
{
    /// <summary>Whether the role has been granted the permission.</summary>
    public bool IsGranted { get; init; }
}
