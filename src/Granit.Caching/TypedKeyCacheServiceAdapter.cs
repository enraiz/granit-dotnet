using Microsoft.Extensions.Caching.Distributed;

namespace Granit.Caching;

/// <summary>
/// Adaptateur qui implémente <see cref="ICacheService{TCacheItem, TKey}"/> en déléguant
/// à un <see cref="ICacheService{TCacheItem}"/> sous-jacent.
/// La clé <typeparamref name="TKey"/> est convertie en <see cref="string"/> via <c>key.ToString()</c>.
/// </summary>
/// <remarks>
/// Cet adaptateur est automatiquement compatible avec tous les fournisseurs
/// (Memory, Redis, Hybrid) sans modification.
/// Il est enregistré en DI pour tous les <c>ICacheService&lt;T, TKey&gt;</c>.
/// </remarks>
/// <param name="inner">Service de cache sous-jacent (clé string).</param>
public sealed class TypedKeyCacheServiceAdapter<TCacheItem, TKey>(ICacheService<TCacheItem> inner) : ICacheService<TCacheItem, TKey>
    where TCacheItem : class
    where TKey : notnull
{
    private readonly ICacheService<TCacheItem> _inner = inner;

    /// <inheritdoc/>
    public Task<TCacheItem?> GetAsync(string key, CancellationToken cancellationToken = default) =>
        _inner.GetAsync(key, cancellationToken);

    /// <inheritdoc/>
    public Task<TCacheItem?> GetAsync(TKey key, CancellationToken cancellationToken = default) =>
        _inner.GetAsync(key.ToString()!, cancellationToken);

    /// <inheritdoc/>
    public Task<TCacheItem> GetOrAddAsync(
        string key,
        Func<CancellationToken, Task<TCacheItem>> factory,
        DistributedCacheEntryOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _inner.GetOrAddAsync(key, factory, options, cancellationToken);

    /// <inheritdoc/>
    public Task<TCacheItem> GetOrAddAsync(
        TKey key,
        Func<CancellationToken, Task<TCacheItem>> factory,
        DistributedCacheEntryOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _inner.GetOrAddAsync(key.ToString()!, factory, options, cancellationToken);

    /// <inheritdoc/>
    public Task SetAsync(
        string key,
        TCacheItem value,
        DistributedCacheEntryOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _inner.SetAsync(key, value, options, cancellationToken);

    /// <inheritdoc/>
    public Task SetAsync(
        TKey key,
        TCacheItem value,
        DistributedCacheEntryOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _inner.SetAsync(key.ToString()!, value, options, cancellationToken);

    /// <inheritdoc/>
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        _inner.RemoveAsync(key, cancellationToken);

    /// <inheritdoc/>
    public Task RemoveAsync(TKey key, CancellationToken cancellationToken = default) =>
        _inner.RemoveAsync(key.ToString()!, cancellationToken);

    /// <inheritdoc/>
    public Task RefreshAsync(string key, CancellationToken cancellationToken = default) =>
        _inner.RefreshAsync(key, cancellationToken);
}
