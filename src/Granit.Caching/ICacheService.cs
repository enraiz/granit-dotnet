using Microsoft.Extensions.Caching.Distributed;

namespace Granit.Caching;

/// <summary>
/// Service de cache typé au-dessus de <see cref="IDistributedCache"/>.
/// Gère automatiquement la sérialisation JSON, la génération de clés préfixées
/// (<c>{KeyPrefix}:{CacheName}:{userKey}</c>) et la protection stampede via double-check locking.
/// </summary>
/// <typeparam name="TCacheItem">Le type de l'objet mis en cache. Doit être une classe.</typeparam>
/// <example>
/// Injection : <c>ICacheService&lt;UserCacheItem&gt; cache</c>
/// Usage : <c>await cache.GetOrAddAsync(userId.ToString(), async cancellationToken =&gt; await repo.GetAsync(userId, cancellationToken));</c>
/// </example>
public interface ICacheService<TCacheItem> where TCacheItem : class
{
    /// <summary>
    /// Retourne l'élément du cache, ou <c>null</c> s'il n'existe pas ou a expiré.
    /// </summary>
    /// <param name="key">Clé utilisateur (sans préfixe).</param>
    /// <param name="cancellationToken">Token d'annulation.</param>
    Task<TCacheItem?> GetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retourne l'élément du cache ou exécute <paramref name="factory"/> exactement une seule fois
    /// sous concurrence (protection stampede via double-check locking).
    /// Inspiré du pattern <c>remember()</c> de Laravel.
    /// </summary>
    /// <param name="key">Clé utilisateur.</param>
    /// <param name="factory">Fabrique exécutée si l'élément est absent du cache.</param>
    /// <param name="options">Options TTL pour cette entrée. Si <c>null</c>, utilise les options par défaut de <see cref="CachingOptions"/>.</param>
    /// <param name="cancellationToken">Token d'annulation.</param>
    Task<TCacheItem> GetOrAddAsync(
        string key,
        Func<CancellationToken, Task<TCacheItem>> factory,
        DistributedCacheEntryOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stocke un élément dans le cache avec les options spécifiées.
    /// </summary>
    /// <param name="key">Clé utilisateur.</param>
    /// <param name="value">Valeur à stocker.</param>
    /// <param name="options">Options TTL. Si <c>null</c>, utilise les options par défaut.</param>
    /// <param name="cancellationToken">Token d'annulation.</param>
    Task SetAsync(
        string key,
        TCacheItem value,
        DistributedCacheEntryOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Supprime l'entrée du cache identifiée par la clé.
    /// </summary>
    /// <param name="key">Clé utilisateur.</param>
    /// <param name="cancellationToken">Token d'annulation.</param>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rafraîchit la durée de vie glissante d'une entrée sans modifier sa valeur.
    /// </summary>
    /// <param name="key">Clé utilisateur.</param>
    /// <param name="cancellationToken">Token d'annulation.</param>
    Task RefreshAsync(string key, CancellationToken cancellationToken = default);
}
