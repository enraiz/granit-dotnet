using System.Collections.Concurrent;

namespace Granit.Features.Store;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IFeatureStoreReader"/> and <see cref="IFeatureStoreWriter"/>.
/// Suitable for testing and development environments.
/// Values are lost on application restart.
/// </summary>
internal sealed class InMemoryFeatureStore : IFeatureStoreReader, IFeatureStoreWriter
{
    private readonly ConcurrentDictionary<string, string> _store =
        new(StringComparer.Ordinal);

    /// <inheritdoc/>
    public Task<string?> GetOrNullAsync(string featureName, string? tenantId, CancellationToken ct = default)
    {
        _store.TryGetValue(BuildKey(featureName, tenantId), out string? value);
        return Task.FromResult(value);
    }

    /// <inheritdoc/>
    public Task SetAsync(string featureName, string? tenantId, string value, CancellationToken ct = default)
    {
        _store[BuildKey(featureName, tenantId)] = value;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task DeleteAsync(string featureName, string? tenantId, CancellationToken ct = default)
    {
        _store.TryRemove(BuildKey(featureName, tenantId), out _);
        return Task.CompletedTask;
    }

    private static string BuildKey(string featureName, string? tenantId) =>
        tenantId is not null
            ? $"t:{tenantId}:{featureName}"
            : $"g:{featureName}";
}
