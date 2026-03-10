using Microsoft.AspNetCore.Authentication;

namespace Granit.Authentication.ApiKeys.Options;

/// <summary>
/// Options for the API key authentication handler.
/// </summary>
public sealed class ApiKeyOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// Cache TTL for API key lookups. Set to <see cref="TimeSpan.Zero"/> to disable caching.
    /// Default: 5 minutes.
    /// </summary>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Whether to update <see cref="ApiKeyEntry.LastUsedAt"/> on each request.
    /// Disable in high-throughput scenarios and use batched updates instead.
    /// Default: <c>true</c>.
    /// </summary>
    public bool TrackLastUsed { get; set; } = true;
}
