using Microsoft.Extensions.Caching.Distributed;

namespace Granit.Caching;

/// <summary>
/// Service de cache typé avec clé de type <typeparamref name="TKey"/> (ABP-style).
/// La clé est automatiquement convertie en <see cref="string"/> via <c>key.ToString()</c>.
/// Hérite de <see cref="ICacheService{TCacheItem}"/> pour la compatibilité avec les clés string.
/// </summary>
/// <typeparam name="TCacheItem">Le type de l'objet mis en cache.</typeparam>
/// <typeparam name="TKey">Le type de la clé. Doit implémenter <c>ToString()</c> de manière significative.</typeparam>
/// <example>
/// Injection : <c>ICacheService&lt;UserCacheItem, Guid&gt; cache</c>
/// Usage : <c>await cache.GetOrAddAsync(userId, async cancellationToken =&gt; await repo.GetAsync(userId, cancellationToken));</c>
/// </example>
public interface ICacheService<TCacheItem, TKey> : ICacheService<TCacheItem>
    where TCacheItem : class
    where TKey : notnull
{
    /// <summary>
    /// Retourne l'élément du cache, ou <c>null</c> si absent. La clé est convertie via <c>key.ToString()</c>.
    /// </summary>
    Task<TCacheItem?> GetAsync(TKey key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pattern "remember" avec clé typée. La clé est convertie via <c>key.ToString()</c>.
    /// </summary>
    Task<TCacheItem> GetOrAddAsync(
        TKey key,
        Func<CancellationToken, Task<TCacheItem>> factory,
        DistributedCacheEntryOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stocke un élément avec une clé typée. La clé est convertie via <c>key.ToString()</c>.
    /// </summary>
    Task SetAsync(
        TKey key,
        TCacheItem value,
        DistributedCacheEntryOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Supprime l'entrée identifiée par la clé typée.
    /// </summary>
    Task RemoveAsync(TKey key, CancellationToken cancellationToken = default);
}
