namespace Granit.Identity.Endpoints.Dtos;

/// <summary>
/// Response for the user cache stats endpoint.
/// </summary>
/// <param name="TotalEntries">Total number of cached entries.</param>
/// <param name="StaleEntries">Number of entries past the staleness threshold.</param>
/// <param name="OldestSyncAt">Timestamp of the oldest sync, or null if cache is empty.</param>
/// <param name="NewestSyncAt">Timestamp of the newest sync, or null if cache is empty.</param>
public sealed record IdentityUserCacheStatsResponse(
    int TotalEntries,
    int StaleEntries,
    DateTimeOffset? OldestSyncAt,
    DateTimeOffset? NewestSyncAt);
