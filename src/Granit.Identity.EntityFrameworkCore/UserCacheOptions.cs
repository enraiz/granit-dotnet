namespace Granit.Identity.EntityFrameworkCore;

/// <summary>
/// Configuration options for the identity user cache.
/// Bind to the <c>IdentityUserCache</c> configuration section.
/// </summary>
public sealed class UserCacheOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "IdentityUserCache";

    /// <summary>
    /// Duration after which a cached user entry is considered stale and eligible for re-fetch
    /// from the identity provider. Default: 24 hours.
    /// </summary>
    public TimeSpan StalenessThreshold { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Enables automatic cache sync on authenticated HTTP requests using JWT claims.
    /// When enabled, <see cref="Middleware.UserCacheSyncMiddleware"/> upserts the current user
    /// from claims on each request if the cached entry is stale or missing. Default: <c>true</c>.
    /// </summary>
    public bool EnableLoginTimeSync { get; set; } = true;
}
