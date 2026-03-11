using Granit.Caching;
using Granit.Caching.Hybrid.Options;
using Granit.Caching.Internal;
using Granit.Caching.Options;
using Granit.Timing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Granit.Caching.Hybrid;

/// <summary>
/// Implémentation de <see cref="ICacheService{TCacheItem}"/> via <see cref="HybridCache"/> (.NET 9).
/// Fournit un cache à deux niveaux : L1 (mémoire locale par pod) + L2 (<c>IDistributedCache</c>, Redis).
/// </summary>
/// <remarks>
/// <para>
/// Comportement L1+L2 sur Kubernetes multi-pods :
/// <list type="bullet">
///   <item>Lecture L1 &lt; 1 ms (mémoire locale du pod)</item>
///   <item>Lecture L2 ~2 ms (Redis partagé entre pods)</item>
///   <item>Cache miss complet : appel de la factory, écriture sur L1+L2</item>
/// </list>
/// </para>
/// <para>
/// Invalidation : <see cref="RemoveAsync(string, CancellationToken)"/> efface L2 (Redis) et le L1
/// du pod appelant. Les L1 des autres pods expirent au maximum après
/// <see cref="HybridCachingOptions.LocalCacheExpiration"/> (30 s par défaut).
/// </para>
/// <para>
/// Protection stampede : native dans <c>HybridCache</c>, aucune <c>SemaphoreSlim</c> nécessaire.
/// </para>
/// <para>
/// Chiffrement ISO 27001 : non supporté par ce fournisseur. <c>HybridCache</c> gère la sérialisation
/// vers L2 en interne — il n'est pas possible d'y intercaler un chiffrement <c>byte[]</c>.
/// Pour les données sensibles nécessitant un chiffrement au repos, utiliser
/// <c>GranitCachingRedisModule</c> (fournisseur Redis pur avec <see cref="ICacheValueEncryptor"/>).
/// </para>
/// </remarks>
/// <typeparam name="TCacheItem">Type de l'élément mis en cache. Doit être une classe.</typeparam>
/// <remarks>
/// Initialise une nouvelle instance de <see cref="HybridCacheService{TCacheItem}"/>.
/// </remarks>
/// <param name="hybridCache">Cache hybride L1+L2 fourni par le runtime .NET 9.</param>
/// <param name="options">Options globales du cache.</param>
/// <param name="logger">Logger structuré.</param>
/// <param name="clock">Horloge UTC pour le calcul des expirations absolues.</param>
public partial class HybridCacheService<TCacheItem>(
    HybridCache hybridCache,
    IOptions<CachingOptions> options,
    ILogger<HybridCacheService<TCacheItem>> logger,
    IClock clock) : ICacheService<TCacheItem>
    where TCacheItem : class
{
    private readonly HybridCache _hybridCache = hybridCache;
    private readonly IOptions<CachingOptions> _options = options;
    private readonly ILogger<HybridCacheService<TCacheItem>> _logger = logger;
    private readonly IClock _clock = clock;
    private readonly string _cacheName = CacheNameProvider.GetCacheName(typeof(TCacheItem));

    /// <inheritdoc/>
    public async Task<TCacheItem?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        string compositeKey = BuildKey(key);
        LogGet(_logger, compositeKey);

        // HybridCache.GetOrCreateAsync ne permet pas de faire un "Get seul" natif.
        // On passe une factory qui retourne null pour simuler un GetOrDefault.
        // Le résultat null est mis en cache brièvement (L1 TTL court) — comportement acceptable.
        TCacheItem? result = await _hybridCache.GetOrCreateAsync<TCacheItem?>(
            compositeKey,
            _ => ValueTask.FromResult<TCacheItem?>(null),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return result;
    }

    /// <inheritdoc/>
    public async Task<TCacheItem> GetOrAddAsync(
        string key,
        Func<CancellationToken, Task<TCacheItem>> factory,
        DistributedCacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        string compositeKey = BuildKey(key);
        LogGetOrAdd(_logger, compositeKey);

        HybridCacheEntryOptions? hybridOptions = options is not null
            ? BuildHybridOptions(options)
            : null;

        TCacheItem result = await _hybridCache.GetOrCreateAsync(
            compositeKey,
            async (innerCt) =>
            {
                LogFactory(_logger, compositeKey);
                TCacheItem value = await factory(innerCt).ConfigureAwait(false);
                return value;
            },
            hybridOptions,
            cancellationToken: cancellationToken);

        return result;
    }

    /// <inheritdoc/>
    public async Task SetAsync(
        string key,
        TCacheItem value,
        DistributedCacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        string compositeKey = BuildKey(key);
        LogSet(_logger, compositeKey);

        HybridCacheEntryOptions? hybridOptions = options is not null
            ? BuildHybridOptions(options)
            : null;

        await _hybridCache.SetAsync(compositeKey, value, hybridOptions, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        string compositeKey = BuildKey(key);
        LogRemove(_logger, compositeKey);

        await _hybridCache.RemoveAsync(compositeKey, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task RefreshAsync(string key, CancellationToken cancellationToken = default)
    {
        // HybridCache ne supporte pas nativement le refresh (sliding expiration sur IDistributedCache).
        // Un SetAsync avec la valeur actuelle est l'équivalent fonctionnel.
        string compositeKey = BuildKey(key);
        LogRefreshNotSupported(_logger, compositeKey);

        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "HybridCache GET {Key}")]
    private static partial void LogGet(ILogger logger, string key);

    [LoggerMessage(Level = LogLevel.Debug, Message = "HybridCache GET_OR_ADD {Key}")]
    private static partial void LogGetOrAdd(ILogger logger, string key);

    [LoggerMessage(Level = LogLevel.Debug, Message = "HybridCache FACTORY {Key}")]
    private static partial void LogFactory(ILogger logger, string key);

    [LoggerMessage(Level = LogLevel.Debug, Message = "HybridCache SET {Key}")]
    private static partial void LogSet(ILogger logger, string key);

    [LoggerMessage(Level = LogLevel.Debug, Message = "HybridCache REMOVE {Key}")]
    private static partial void LogRemove(ILogger logger, string key);

    [LoggerMessage(Level = LogLevel.Warning, Message = "HybridCache REFRESH not natively supported for {Key}. Use GetOrAddAsync instead.")]
    private static partial void LogRefreshNotSupported(ILogger logger, string key);

    private string BuildKey(string userKey) =>
        $"{_options.Value.KeyPrefix}:{_cacheName}:{userKey}";

    private HybridCacheEntryOptions BuildHybridOptions(DistributedCacheEntryOptions distributed) =>
        new()
        {
            Expiration = distributed.AbsoluteExpirationRelativeToNow
                ?? (distributed.AbsoluteExpiration.HasValue
                    ? distributed.AbsoluteExpiration.Value - _clock.Now
                    : null),
            LocalCacheExpiration = distributed.SlidingExpiration,
        };
}
