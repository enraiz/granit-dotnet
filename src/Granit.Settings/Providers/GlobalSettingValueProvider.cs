using Granit.Caching;
using Granit.Settings.Definitions;
using Granit.Settings.Options;
using Granit.Settings.Values;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Granit.Settings.Providers;

/// <summary>
/// Global settings provider (application scope, no tenant/user isolation).
/// Caches values read from <see cref="ISettingStore"/> (order = 300).
/// </summary>
public sealed class GlobalSettingValueProvider(
    ISettingStore store,
    ICacheService<SettingValue> cache,
    IOptions<SettingsOptions> options) : ISettingValueProvider
{
    /// <summary>Global provider identifier.</summary>
    public const string ProviderName = "G";

    private readonly ISettingStore _store = store;
    private readonly ICacheService<SettingValue> _cache = cache;
    private readonly IOptions<SettingsOptions> _options = options;

    /// <inheritdoc/>
    public string Name => ProviderName;

    /// <inheritdoc/>
    public int Order => 300;

    /// <inheritdoc/>
    public async Task<SettingValue?> GetOrNullAsync(SettingDefinition definition, CancellationToken ct = default)
    {
        string cacheKey = SettingCacheKey.Build(ProviderName, null, definition.Name);
        DistributedCacheEntryOptions cacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = _options.Value.CacheExpiration
        };

        return await _cache.GetOrAddAsync(
            cacheKey,
            async innerCt =>
            {
                SettingValue? stored = await _store.GetOrNullAsync(
                    definition.Name, ProviderName, null, innerCt).ConfigureAwait(false);
                // Sentinel to distinguish "stored null" from "absent from cache"
                return stored ?? new SettingValue(definition.Name, ProviderName, null, null);
            },
            cacheOptions,
            ct) is { Value: not null } hit
            ? hit
            : null;
    }

    /// <inheritdoc/>
    public async Task SetAsync(SettingDefinition definition, string? value, CancellationToken ct = default)
    {
        await _store.SetAsync(definition.Name, ProviderName, null, value, ct).ConfigureAwait(false);
        await _cache.RemoveAsync(SettingCacheKey.Build(ProviderName, null, definition.Name), ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task ClearAsync(SettingDefinition definition, CancellationToken ct = default)
    {
        await _store.DeleteAsync(definition.Name, ProviderName, null, ct).ConfigureAwait(false);
        await _cache.RemoveAsync(SettingCacheKey.Build(ProviderName, null, definition.Name), ct).ConfigureAwait(false);
    }
}
