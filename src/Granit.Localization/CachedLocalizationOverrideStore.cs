using Granit.Core.MultiTenancy;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Granit.Localization;

/// <summary>
/// Caching decorator for <see cref="ILocalizationOverrideStoreReader"/> and <see cref="ILocalizationOverrideStoreWriter"/>.
/// </summary>
/// <remarks>
/// Wraps any inner store (typically EF Core, registered as keyed service <see cref="RawStoreKey"/>)
/// with an <see cref="IMemoryCache"/> L1 cache, making read access synchronous-safe for use inside
/// <c>IStringLocalizer</c>.
/// <para>
/// An <see cref="AsyncServiceScope"/> is created per DB operation so that the underlying store
/// (Scoped) is resolved with its full dependency graph, including <c>AuditedEntityInterceptor</c>
/// for HDS audit compliance on write operations.
/// </para>
/// <para>
/// Cache is invalidated on every write or delete.
/// Cache keys are scoped per tenant when <see cref="ICurrentTenant"/> is registered.
/// </para>
/// </remarks>
internal sealed class CachedLocalizationOverrideStore(
    IMemoryCache memoryCache,
    IOptions<LocalizationOverridesCacheOptions> options,
    IServiceScopeFactory scopeFactory,
    IServiceProvider serviceProvider) : ILocalizationOverrideStoreReader, ILocalizationOverrideStoreWriter
{
    private readonly LocalizationOverridesCacheOptions _options = options.Value;

    /// <summary>
    /// Keyed service key used to register the underlying (non-cached) store.
    /// The EF Core module registers <c>EfCoreLocalizationOverrideStore</c> under this key.
    /// </summary>
    internal const string RawStoreKey = "localization-override-raw";

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<string, string>> GetOverridesAsync(
        string resourceName, string culture, CancellationToken ct = default)
    {
        string cacheKey = BuildCacheKey(resourceName, culture);

        if (memoryCache.TryGetValue(cacheKey, out IReadOnlyDictionary<string, string>? cached) && cached is not null)
        {
            return Task.FromResult(cached);
        }

        return LoadAndCacheAsync(cacheKey, resourceName, culture, ct);
    }

    /// <inheritdoc />
    public async Task SetOverrideAsync(
        string resourceName, string culture, string key, string value, CancellationToken ct = default)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        ILocalizationOverrideStoreWriter inner =
            scope.ServiceProvider.GetRequiredKeyedService<ILocalizationOverrideStoreWriter>(RawStoreKey);

        await inner.SetOverrideAsync(resourceName, culture, key, value, ct).ConfigureAwait(false);
        memoryCache.Remove(BuildCacheKey(resourceName, culture));
    }

    /// <inheritdoc />
    public async Task RemoveOverrideAsync(
        string resourceName, string culture, string key, CancellationToken ct = default)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        ILocalizationOverrideStoreWriter inner =
            scope.ServiceProvider.GetRequiredKeyedService<ILocalizationOverrideStoreWriter>(RawStoreKey);

        await inner.RemoveOverrideAsync(resourceName, culture, key, ct).ConfigureAwait(false);
        memoryCache.Remove(BuildCacheKey(resourceName, culture));
    }

    private async Task<IReadOnlyDictionary<string, string>> LoadAndCacheAsync(
        string cacheKey, string resourceName, string culture, CancellationToken ct)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        ILocalizationOverrideStoreReader? inner =
            scope.ServiceProvider.GetKeyedService<ILocalizationOverrideStoreReader>(RawStoreKey);

        // No raw store registered (EF Core package not installed): fall back to empty overrides
        // so the localizer resolves translations from embedded JSON files transparently.
        IReadOnlyDictionary<string, string> overrides = inner is not null
            ? await inner.GetOverridesAsync(resourceName, culture, ct).ConfigureAwait(false)
            : new Dictionary<string, string>(StringComparer.Ordinal);

        MemoryCacheEntryOptions entryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(_options.CacheTtl);

        memoryCache.Set(cacheKey, overrides, entryOptions);
        return overrides;
    }

    private string BuildCacheKey(string resourceName, string culture)
    {
        ICurrentTenant? currentTenant = serviceProvider.GetService<ICurrentTenant>();
        string tenantSegment = currentTenant?.IsAvailable == true
            ? currentTenant.Id!.Value.ToString()
            : "host";

        return $"localization:{tenantSegment}:{resourceName}:{culture}";
    }
}
