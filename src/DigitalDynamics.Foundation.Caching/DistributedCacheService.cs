using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DigitalDynamics.Foundation.Caching;

/// <summary>
/// Implémentation de <see cref="ICacheService{TCacheItem}"/> au-dessus de <see cref="IDistributedCache"/>.
/// </summary>
/// <remarks>
/// Fonctionnalités :
/// <list type="bullet">
///   <item>Sérialisation/désérialisation JSON automatique via <c>System.Text.Json</c></item>
///   <item>Clé composite : <c>{KeyPrefix}:{CacheName}:{userKey}</c></item>
///   <item>Protection stampede : double-check locking + <see cref="SemaphoreSlim"/> dans <see cref="IMemoryCache"/> dédié (TTL 30 s)</item>
///   <item>Chiffrement AES-256-CBC opt-in via <see cref="CacheEncryptedAttribute"/> ou <see cref="CachingOptions.EncryptValues"/></item>
/// </list>
/// Le <see cref="IMemoryCache"/> injecté est dédié aux verrous stampede (clé DI : <c>DigitalDynamics.Foundation.Caching.Locks</c>)
/// et est séparé du cache mémoire applicatif pour éviter les interférences.
/// </remarks>
public partial class DistributedCacheService<TCacheItem> : ICacheService<TCacheItem>
    where TCacheItem : class
{
    internal const string LockCacheKey = "DigitalDynamics.Foundation.Caching.Locks";

    private readonly IDistributedCache _cache;
    private readonly IMemoryCache _lockCache;
    private readonly ICacheValueEncryptor _encryptor;
    private readonly IOptions<CachingOptions> _options;
    private readonly ILogger<DistributedCacheService<TCacheItem>> _logger;
    private readonly string _cacheName;
    private readonly bool _shouldEncrypt;

    /// <param name="cache">Le fournisseur de cache distribué (Memory ou Redis).</param>
    /// <param name="lockCache">Cache mémoire dédié aux verrous stampede.</param>
    /// <param name="encryptor">Chiffreur AES (no-op en dev, AES-256 en prod).</param>
    /// <param name="options">Options globales du cache.</param>
    /// <param name="logger">Logger pour le diagnostic.</param>
    public DistributedCacheService(
        IDistributedCache cache,
        [FromKeyedServices(LockCacheKey)] IMemoryCache lockCache,
        ICacheValueEncryptor encryptor,
        IOptions<CachingOptions> options,
        ILogger<DistributedCacheService<TCacheItem>> logger)
    {
        _cache = cache;
        _lockCache = lockCache;
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
        byte[]? bytes = await _cache.GetAsync(compositeKey, ct);

        if (bytes is null)
        {
            return null;
        }

        if (_shouldEncrypt)
        {
            bytes = _encryptor.Decrypt(bytes);
        }

        return JsonSerializer.Deserialize<TCacheItem>(bytes, _options.Value.JsonOptions);
    }

    /// <inheritdoc/>
    public async Task<TCacheItem> GetOrAddAsync(
        string key,
        Func<CancellationToken, Task<TCacheItem>> factory,
        DistributedCacheEntryOptions? options = null,
        CancellationToken ct = default)
    {
        // 1. Vérification rapide sans verrou (chemin chaud — évite la contention)
        TCacheItem? cached = await GetAsync(key, ct);
        if (cached is not null)
        {
            return cached;
        }

        // 2. Acquisition du verrou stocké dans IMemoryCache (TTL 30 s — auto-nettoyage par le GC)
        SemaphoreSlim semaphore = GetOrCreateLock(BuildKey(key));
        await semaphore.WaitAsync(ct);
        try
        {
            // 3. Double-check locking : un autre thread peut avoir rempli le cache pendant l'attente
            cached = await GetAsync(key, ct);
            if (cached is not null)
            {
                return cached;
            }

            // 4. Exécution de la factory (garantie une seule fois sous concurrence)
            TCacheItem value = await factory(ct);
            await SetAsync(key, value, options, ct);

            LogCacheMiss(_logger, BuildKey(key));

            return value;
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task SetAsync(
        string key,
        TCacheItem value,
        DistributedCacheEntryOptions? options = null,
        CancellationToken ct = default)
    {
        string compositeKey = BuildKey(key);
        DistributedCacheEntryOptions entryOptions = options ?? BuildDefaultOptions();

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(value, _options.Value.JsonOptions);

        if (_shouldEncrypt)
        {
            bytes = _encryptor.Encrypt(bytes);
        }

        await _cache.SetAsync(compositeKey, bytes, entryOptions, ct);

        LogCacheSet(_logger, compositeKey);
    }

    /// <inheritdoc/>
    public Task RemoveAsync(string key, CancellationToken ct = default) =>
        _cache.RemoveAsync(BuildKey(key), ct);

    /// <inheritdoc/>
    public Task RefreshAsync(string key, CancellationToken ct = default) =>
        _cache.RefreshAsync(BuildKey(key), ct);

    private string BuildKey(string userKey) =>
        $"{_options.Value.KeyPrefix}:{_cacheName}:{userKey}";

    private DistributedCacheEntryOptions BuildDefaultOptions()
    {
        CachingOptions opts = _options.Value;
        DistributedCacheEntryOptions entry = new();

        if (opts.DefaultAbsoluteExpirationRelativeToNow.HasValue)
        {
            entry.AbsoluteExpirationRelativeToNow = opts.DefaultAbsoluteExpirationRelativeToNow;
        }

        if (opts.DefaultSlidingExpiration.HasValue)
        {
            entry.SlidingExpiration = opts.DefaultSlidingExpiration;
        }

        return entry;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cache miss resolved via factory: {Key}")]
    private static partial void LogCacheMiss(ILogger logger, string key);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cache set: {Key}")]
    private static partial void LogCacheSet(ILogger logger, string key);

    private SemaphoreSlim GetOrCreateLock(string compositeKey) =>
        _lockCache.GetOrCreate(
            $"__lock:{compositeKey}",
            entry =>
            {
                // TTL court : le GC libère automatiquement les verrous après 30 s
                // Évite la fuite mémoire d'un ConcurrentDictionary non borné
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
                // Taille requise quand SizeLimit est configuré sur l'IMemoryCache
                entry.Size = 1;
                return new SemaphoreSlim(1, 1);
            })!;
}
