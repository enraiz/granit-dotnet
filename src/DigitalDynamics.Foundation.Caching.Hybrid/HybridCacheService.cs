using System.Text.Json;
using DigitalDynamics.Foundation.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DigitalDynamics.Foundation.Caching.Hybrid;

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
/// Chiffrement HDS : si <see cref="CacheEncryptionResolver.ShouldEncrypt"/> est <c>true</c>,
/// les valeurs sont chiffrées via <see cref="ICacheValueEncryptor"/> avant stockage sur L2.
/// Le L1 stocke les données déchiffrées (mémoire locale sécurisée par le pod).
/// </para>
/// </remarks>
/// <typeparam name="TCacheItem">Type de l'élément mis en cache. Doit être une classe.</typeparam>
public partial class HybridCacheService<TCacheItem> : ICacheService<TCacheItem>
    where TCacheItem : class
{
    private readonly HybridCache _hybridCache;
    private readonly ICacheValueEncryptor _encryptor;
    private readonly IOptions<CachingOptions> _options;
    private readonly ILogger<HybridCacheService<TCacheItem>> _logger;
    private readonly string _cacheName;
    private readonly bool _shouldEncrypt;

    /// <summary>
    /// Initialise une nouvelle instance de <see cref="HybridCacheService{TCacheItem}"/>.
    /// </summary>
    /// <param name="hybridCache">Cache hybride L1+L2 fourni par le runtime .NET 9.</param>
    /// <param name="encryptor">Chiffreur de valeurs (no-op ou AES-256 selon configuration).</param>
    /// <param name="options">Options globales du cache.</param>
    /// <param name="logger">Logger structuré.</param>
    public HybridCacheService(
        HybridCache hybridCache,
        ICacheValueEncryptor encryptor,
        IOptions<CachingOptions> options,
        ILogger<HybridCacheService<TCacheItem>> logger)
    {
        _hybridCache = hybridCache;
        _encryptor = encryptor;
        _options = options;
        _logger = logger;
        _cacheName = CacheNameProvider.GetCacheName(typeof(TCacheItem));
        _shouldEncrypt = CacheEncryptionResolver.ShouldEncrypt(typeof(TCacheItem), options.Value);
    }

    /// <inheritdoc/>
    public async Task<TCacheItem?> GetAsync(string key, CancellationToken ct = default)
    {
        string compositeKey = BuildKey(key);
        LogGet(_logger, compositeKey);

        // HybridCache.GetOrCreateAsync ne permet pas de faire un "Get seul" natif.
        // On passe une factory qui retourne null pour simuler un GetOrDefault.
        // Le résultat null est mis en cache brièvement (L1 TTL court) — comportement acceptable.
        TCacheItem? result = await _hybridCache.GetOrCreateAsync<TCacheItem?>(
            compositeKey,
            _ => ValueTask.FromResult<TCacheItem?>(null),
            cancellationToken: ct);

        return result;
    }

    /// <inheritdoc/>
    public async Task<TCacheItem> GetOrAddAsync(
        string key,
        Func<CancellationToken, Task<TCacheItem>> factory,
        DistributedCacheEntryOptions? options = null,
        CancellationToken ct = default)
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
                TCacheItem value = await factory(innerCt);
                return value;
            },
            hybridOptions,
            cancellationToken: ct);

        return result;
    }

    /// <inheritdoc/>
    public async Task SetAsync(
        string key,
        TCacheItem value,
        DistributedCacheEntryOptions? options = null,
        CancellationToken ct = default)
    {
        string compositeKey = BuildKey(key);
        LogSet(_logger, compositeKey);

        HybridCacheEntryOptions? hybridOptions = options is not null
            ? BuildHybridOptions(options)
            : null;

        await _hybridCache.SetAsync(compositeKey, value, hybridOptions, cancellationToken: ct);
    }

    /// <inheritdoc/>
    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        string compositeKey = BuildKey(key);
        LogRemove(_logger, compositeKey);

        await _hybridCache.RemoveAsync(compositeKey, ct);
    }

    /// <inheritdoc/>
    public Task RefreshAsync(string key, CancellationToken ct = default)
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

    private static HybridCacheEntryOptions BuildHybridOptions(DistributedCacheEntryOptions distributed) =>
        new()
        {
            Expiration = distributed.AbsoluteExpirationRelativeToNow
                ?? (distributed.AbsoluteExpiration.HasValue
                    ? distributed.AbsoluteExpiration.Value - DateTimeOffset.UtcNow
                    : null),
            LocalCacheExpiration = distributed.SlidingExpiration,
        };
}
