namespace Granit.Identity.Internal;

/// <summary>
/// Null-object implementation of <see cref="IUserCacheStats"/>.
/// Returns zeroes / nulls for all operations.
/// </summary>
internal sealed class NullUserCacheStats : IUserCacheStats
{
    /// <inheritdoc/>
    public Task<int> GetCountAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(0);

    /// <inheritdoc/>
    public Task<int> GetStaleCountAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(0);

    /// <inheritdoc/>
    public Task<(DateTimeOffset? Oldest, DateTimeOffset? Newest)> GetSyncRangeAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult<(DateTimeOffset? Oldest, DateTimeOffset? Newest)>((null, null));
}
